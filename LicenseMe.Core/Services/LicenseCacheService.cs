using LicenseMe.Core.Cache;
using LicenseMe.Core.Domain;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Core.Services;

internal sealed class LicenseCacheService(
    ILogger<LicenseCacheService> logger,
    ILicenseCacheStore cacheStore,
    IOsiClient osiClient,
    ILicenseCollectionWriter licenseWriter,
    LicenseCacheOptions cacheOptions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!cacheOptions.Enabled)
            return;

        if (await HasCacheExpiredOrIsInvalid(stoppingToken))
            await RefreshAsync(stoppingToken);
        else
        {
            var list = await GetAllLicenses(true, stoppingToken);
            licenseWriter.SetLicenses(list.AsReadOnly());
        }
        
        using var timer = new PeriodicTimer(cacheOptions.RefreshInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (await HasCacheExpiredOrIsInvalid(stoppingToken))
            {
                await cacheStore.PurgeExpiredAsync(stoppingToken);
                await RefreshAsync(stoppingToken);
            }
        }
    }

    private async Task<OsiLicense[]> GetAllLicenses(bool fromCache = true, CancellationToken ct = default)
    {
        var asyncEnumerable = fromCache 
            ? cacheStore.GetAllAsync(ct)
            : osiClient.GetAllLicensesAsyncEnumerable(ct);

        Func<string, CancellationToken, Task<OsiLicense?>> singleLicense = fromCache
            ? cacheStore.GetByOsiIdAsync
            : osiClient.GetByOsiIdAsync;
        var list = new List<OsiLicense>();
        await foreach (var license in asyncEnumerable.WithCancellation(ct))
        {
            if(license is null)
                continue;
            if((await singleLicense(license.Id, ct)) is { } fullLicense)
                list.Add(fullLicense);
        }
        return list.ToArray();
    }

    private async Task<bool> HasCacheExpiredOrIsInvalid(CancellationToken ct = default) =>
        (await cacheStore.GetIntegrityAsync(ct)) is { IsExpired: true } or { IsValid: false };

    private async Task RefreshAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Cache refresh cycle started");
        try
        {
            var licenses = await GetAllLicenses(false, ct);

            await cacheStore.UpsertBulkAsync(licenses, ct);
            licenseWriter.SetLicenses(licenses.AsReadOnly());
            logger.LogInformation("Cache refresh completed — {Count} licenses stored", licenses.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cache refresh cycle failed");
        }
    }
}