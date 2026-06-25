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
            await foreach (var license in GetLicenses(true, stoppingToken))
            {
                if(license is null || string.IsNullOrWhiteSpace(license.LicenseText))
                    continue;
                logger.LogDebug("Adding license {LicenseId}", license.SpdxId);
                licenseWriter.SetLicense(license);
            }
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

    private IAsyncEnumerable<OsiLicense?> GetLicenses(bool fromCache = true, CancellationToken ct = default)
    => fromCache
        ? cacheStore.GetAllAsync(ct)
        : osiClient.GetAllLicensesAsyncEnumerable(ct);


    private async Task<bool> HasCacheExpiredOrIsInvalid(CancellationToken ct = default) =>
        (await cacheStore.GetIntegrityAsync(ct)) is { IsExpired: true } or { IsValid: false };

    private async Task RefreshAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Cache refresh cycle started");
        try
        {
            await foreach(var license in GetLicenses(false, ct))
            {
                if (string.IsNullOrWhiteSpace(license?.LicenseText)) continue;
                logger.LogDebug("Updating license {LicenseId}", license.SpdxId);
                await cacheStore.UpsertAsync(license, ct);
                licenseWriter.SetLicense(license, true);
            }

            _lastRefresh = DateTimeOffset.UtcNow;

            var config = licenseMeConfig.Value;
            config.LastCacheRefresh = _lastRefresh.ToUnixTimeSeconds();
            await configManager.SaveAsync(config, ct);

            logger.LogInformation("Cache refresh completed — {Count} licenses stored", await cacheStore.GetCountAsync(ct));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cache refresh cycle failed");
        }
    }
}