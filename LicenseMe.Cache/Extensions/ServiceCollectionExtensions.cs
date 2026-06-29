using Microsoft.Extensions.DependencyInjection;

namespace LicenseMe.Cache.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection collection)
    {
        public IServiceCollection AddLicenseCache()
        {
            return collection;
        }
    }
}