using LicenseMe.Core.Cache;
using LicenseMe.Core.Domain;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Core.Services;

internal sealed class LicenseCacheService(
    ILogger<LicenseCacheService> logger,
    ILicenseCacheStore cacheStore,
    IOsiClient osiClient,
    ILicenseCollectionWriter licenseWriter,
    IOptions<LicenseMeConfig> licenseMeConfig,
    IConfigManager configManager,
    LicenseCacheOptions cacheOptions) : BackgroundService
{
    private DateTimeOffset _lastRefresh;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!cacheOptions.Enabled)
            return;

        var persistedConfig = await configManager.LoadAsync(stoppingToken);
        _lastRefresh = DateTimeOffset.FromUnixTimeSeconds(persistedConfig.LastCacheRefresh);

        if (await HasCacheExpiredOrIsInvalid(stoppingToken))
            await RefreshAsync(stoppingToken);
        else
        {
            var list = await GetAllLicenses(true, stoppingToken);
            licenseWriter.SetLicenses(list.AsReadOnly());
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = _lastRefresh.Add(cacheOptions.RefreshInterval) - DateTimeOffset.UtcNow;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);

            if (await HasCacheExpiredOrIsInvalid(stoppingToken))
            {
                await cacheStore.PurgeExpiredAsync(stoppingToken);
                await RefreshAsync(stoppingToken);
            }
        }
    }


    private async Task<OsiLicense[]> GetAllLicenses(bool fromCache = true, CancellationToken ct = default)
    {
        var list = new List<OsiLicense>();
        await foreach (var license in (fromCache
                           ? cacheStore.GetAllAsync(ct)
                           : osiClient.GetAllLicensesAsyncEnumerable(ct)).WithCancellation(ct))
        {
            if (string.IsNullOrWhiteSpace(license?.LicenseText)) continue;
            
            logger.LogInformation("Adding license {LicenseId}", license.SpdxId);
            list.AddRange(license);
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

            _lastRefresh = DateTimeOffset.UtcNow;

            var config = licenseMeConfig.Value;
            config.LastCacheRefresh = _lastRefresh.ToUnixTimeSeconds();
            await configManager.SaveAsync(config, ct);

            logger.LogInformation("Cache refresh completed — {Count} licenses stored", licenses.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cache refresh cycle failed");
        }
    }
}