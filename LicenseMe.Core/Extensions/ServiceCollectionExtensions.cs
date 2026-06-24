using System.Net.Http.Headers;
using System.Reflection;
using LicenseMe.Core.Cache;
using LicenseMe.Core.Domain;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using LicenseMe.Core.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;
using OpenSourceInitiative.LicenseApi.Interfaces;

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
            services.AddSingleton<ILicenseCacheStore, LicenseCacheStore>();
            services.AddSingleton<LicenseRepository>();
            services.AddSingleton<ILicenseRepository>(sp => sp.GetRequiredService<LicenseRepository>());
            services.AddSingleton<ILicenseCollectionWriter>(sp => sp.GetRequiredService<LicenseRepository>());
            services.AddHostedService<LicenseCacheService>();
            services.AddSingleton(new LicenseCacheOptions
            {
                Enabled         = true,
                DatabasePath    = Path.Combine(ConfigManager.ConfigBasePath, AppDomain.CurrentDomain.FriendlyName.ToLower() + ".db"),
                Ttl             = TimeSpan.FromDays(180),
                RefreshInterval = TimeSpan.FromDays(30)
            });
            
            services.AddOsiLicensesClient(options =>
            {
                options.EnableCaching = false;
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
    }
}
