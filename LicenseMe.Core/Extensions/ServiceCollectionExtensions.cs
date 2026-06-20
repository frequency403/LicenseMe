using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using LicenseMe.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSourceInitiative.LicenseApi.Extensions;

namespace LicenseMe.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLicenseMeCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LicenseMeConfig>(configuration.GetSection("LicenseMe"));

        services.AddMemoryCache();
        services.AddOsiLicensesClient(options => options.EnableCaching = true);

        services.AddSingleton<IRepositoryScanner, RepositoryScanner>();
        services.AddSingleton<ILicenseFileDetector, LicenseFileDetector>();
        services.AddSingleton<IReadmeFileDetector, ReadmeFileDetector>();
        services.AddSingleton<ILicenseWriter, LicenseWriter>();
        services.AddSingleton<IReadmeWriter, ReadmeWriter>();
        services.AddSingleton<IConfigManager, ConfigManager>();

        return services;
    }
}
