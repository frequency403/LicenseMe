using System.Net.Http.Headers;
using System.Reflection;
using Karambolo.Extensions.Logging.File;
using LicenseMe.Cache.Context;
using LicenseMe.Cache.Interceptors;
using LicenseMe.Cache.Services;
using LicenseMe.Core.Domain;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using LicenseMe.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;

namespace LicenseMe.Core.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IHostApplicationBuilder hostBuilder)
    {
        public IServiceCollection AddLicenseMeCore()
        {
            hostBuilder.Logging.ClearProviders();
            hostBuilder.Logging.AddFile(options =>
            {
                if(!Directory.Exists(ConfigManager.LogPath))
                    Directory.CreateDirectory(ConfigManager.LogPath);
                options.RootPath = ConfigManager.LogPath;
                options.MaxFileSize = 52428800; // 50 MiB
                options.Files =
                [
                    new LogFileOptions()
                    {
                        Path = ConfigManager.CurrentApplicationLogName
                    }
                ];
            });
           return hostBuilder.Services.AddLicenseMeCore(hostBuilder.Configuration);
        }
    }
    
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the core LicenseMe services and configurations to the service collection.
        /// </summary>
        /// <param name="configuration">
        /// The configuration object used to bind and configure the <see cref="LicenseMeConfig"/> section.
        /// </param>
        /// <returns>
        /// The updated instance of <see cref="IServiceCollection"/> for method chaining.
        /// </returns>
        private IServiceCollection AddLicenseMeCore(IConfiguration configuration)
        {
            var configSection = configuration.GetSection("LicenseMe");
            services.Configure<LicenseMeConfig>(configSection);
            
            services.AddOsiLicensesClient(options =>
            {
                options.UserAgent =
                [
                    new ProductInfoHeaderValue(AppDomain.CurrentDomain.FriendlyName.ToLower(),
                        (Assembly.GetExecutingAssembly().GetName().Version ?? Version.Parse("1.0.0")).ToString(3))
                ];
            });

            var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder();
            if ((configuration.Get<LicenseMeConfig>()?.CachingEnabled ?? false))
            {
                sqliteConnectionStringBuilder.DataSource = ConfigManager.DatabaseFileFullPath;
                sqliteConnectionStringBuilder.Mode = SqliteOpenMode.ReadWriteCreate;
                sqliteConnectionStringBuilder.Pooling = true;
            }
            else
            {
                ConfigManager.ClearTempDirectory();
                var tempDatabasePath = ConfigManager.TempDatabaseFullPath;
                sqliteConnectionStringBuilder.DataSource = tempDatabasePath;
                sqliteConnectionStringBuilder.Mode = SqliteOpenMode.ReadWriteCreate;
                sqliteConnectionStringBuilder.Pooling = true;
                services.AddHostedService<TempDatabaseCleanupService>(sp =>
                    new TempDatabaseCleanupService(tempDatabasePath, sp.GetRequiredService<ILogger<TempDatabaseCleanupService>>()));
            }

            services.AddSingleton<IRepositoryScanner, RepositoryScanner>();
            services.AddSingleton<ILicenseFileDetector, LicenseFileDetector>();
            services.AddSingleton<IReadmeFileDetector, ReadmeFileDetector>();
            services.AddSingleton<ILicenseWriter, LicenseWriter>();
            services.AddSingleton<IReadmeWriter, ReadmeWriter>();
            services.AddSingleton<IConfigManager, ConfigManager>();
            services.AddHostedService<LicenseFetcher>();

            void ConfigureLicenseDbContext(DbContextOptionsBuilder options)
            {
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                    .UseSqlite(sqliteConnectionStringBuilder.ToString(), opt =>
                    {
                        opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    })
                    .AddInterceptors(new SqliteWalModeInterceptor());
            }

            // Both LicensesViewModel and LicenseFetcher create and dispose a LicenseDbContext per
            // operation via IDbContextFactory instead of holding a shared scoped instance - the
            // ViewModel is a singleton and fires a fresh query per keystroke/filter change, and a
            // BackgroundService has no natural per-operation DI scope to hang a Scoped registration
            // off either way, so CreateDbContextAsync is the fit for both.
            services.AddDbContextFactory<LicenseDbContext>(ConfigureLicenseDbContext);
            return services;
        }
    }
}
