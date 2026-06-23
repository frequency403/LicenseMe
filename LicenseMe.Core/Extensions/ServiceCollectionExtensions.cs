using System.Net.Http.Headers;
using System.Reflection;
using LicenseMe.Core.Cache;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using LicenseMe.Core.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;

namespace LicenseMe.Core.Extensions;

public static class ServiceCollectionExtensions
{
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
        public IServiceCollection AddLicenseMeCore(IConfiguration configuration)
        {
            var configSection = configuration.GetSection("LicenseMe");
            services.Configure<LicenseMeConfig>(configSection);

            services.AddSqliteDistributedCache();
            services.AddOsiLicensesClient(options =>
            {
                options.EnableCaching = true;
                options.UserAgent =
                [
                    new ProductInfoHeaderValue(AppDomain.CurrentDomain.FriendlyName.ToLower(),
                        (Assembly.GetExecutingAssembly().GetName().Version ?? Version.Parse("1.0.0")).ToString(3))
                ];
            });
            
            services.AddSingleton<IRepositoryScanner, RepositoryScanner>();
            services.AddSingleton<ILicenseFileDetector, LicenseFileDetector>();
            services.AddSingleton<IReadmeFileDetector, ReadmeFileDetector>();
            services.AddSingleton<ILicenseWriter, LicenseWriter>();
            services.AddSingleton<IReadmeWriter, ReadmeWriter>();
            services.AddSingleton<IConfigManager, ConfigManager>();

            return services;
        }
        
        public IServiceCollection AddSqliteDistributedCache(
            Action<SqliteCacheOptions>? configure = null)
        {
            var options = new SqliteCacheOptions();
            if(configure is not null)
                configure(options);
            else
            {
                options.EnableWalMode = true;
                options.DatabasePath = Path.Combine(ConfigManager.ConfigBasePath, AppDomain.CurrentDomain.FriendlyName.ToLower() + ".db");
            }
            services.AddSingleton<IDistributedCache>(_ => new SqliteDistributedCache(options));
            return services;
        }
    }
}
