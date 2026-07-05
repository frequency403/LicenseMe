using LicenseMe.Cache.Context;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Core.Services;

public class LicenseFetcher(ILogger<LicenseFetcher> logger, IOsiClient osiClient, IOptions<LicenseMeConfig> config, [FromKeyedServices("LicenseReporter")] IProgressReporter<string> progressReporter, IDbContextFactory<LicenseDbContext> dbContextFactory) : BackgroundService
{
    private readonly PeriodicTimer _periodicTimer = new(TimeSpan.FromMinutes(1));

    /// <summary>
    /// Exposed for tests to assert the timer was recalculated correctly, without exposing the timer itself.
    /// </summary>
    internal TimeSpan CurrentPeriod => _periodicTimer.Period;

    private async Task SetAllLicenses(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var license in osiClient.GetAllLicensesAsyncEnumerable(stoppingToken))
            {
                try
                {
                    if(license is null)
                        continue;
                    progressReporter.TryUpdateProgress(license.Name);
                    logger.LogInformation("Fetched license {LicenseSpdxId}", license.SpdxId ?? license.Id);
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync(stoppingToken);
                    await dbContext.Licenses.AddAsync(license, stoppingToken);
                    if(await dbContext.SaveChangesAsync(stoppingToken) <= 0)
                        throw new InvalidOperationException("No changes were saved");
                    progressReporter.TryUpdateProgress(string.Empty);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error while fetching licenses");
                    progressReporter.TryUpdateProgress(e.Message);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while set all licenses");
            progressReporter.TryUpdateProgress(e.Message);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using (var dbContext = await dbContextFactory.CreateDbContextAsync(stoppingToken))
        {
            try
            {
                foreach(var migration in await dbContext.Database.GetPendingMigrationsAsync(stoppingToken))
                {
                    logger.LogWarning("Migration {MigrationId} pending, starting migraton...", migration);
                    await dbContext.Database.MigrateAsync(migration, stoppingToken);
                    logger.LogWarning("Migration {MigrationId} finished successfully", migration);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while migrating licenses database");
                return;
            }
            if (!dbContext.Licenses.Any())
            {
                await SetAllLicenses(stoppingToken);
            }
        }

        try
        {
            do
            {
                await UpdatePeriodicTimerTick(stoppingToken);
                while (await _periodicTimer.WaitForNextTickAsync(stoppingToken))
                {
                    // The read has to fully finish - and its connection close - before any refresh below
                    // opens a write. Only ids are fetched, not full entities, so this stays cheap even
                    // with many expired licenses.
                    List<string> expiredLicenseIds;
                    await using (var readContext = await dbContextFactory.CreateDbContextAsync(stoppingToken))
                    {
                        expiredLicenseIds = await GetExpiredLicenseIdsAsync(readContext, stoppingToken);
                    }

                    foreach (var expiredLicenseId in expiredLicenseIds)
                    {
                        await RefreshLicenseAsync(expiredLicenseId, stoppingToken);
                    }

                    await UpdatePeriodicTimerTick(stoppingToken);
                }

            } while (!stoppingToken.IsCancellationRequested);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while monitoring license expiry");
        }
    }

    /// <summary>
    /// Fetches only the OSI ids of expired licenses from <paramref name="readContext"/>, never the full
    /// entity graph, keeping memory use flat regardless of how many licenses expired in one tick.
    /// </summary>
    internal async Task<List<string>> GetExpiredLicenseIdsAsync(LicenseDbContext readContext, CancellationToken stoppingToken)
    {
        // Precompute the cutoff instead of writing "DateTime.Now - Timestamp >= Ttl" inline: EF Core keeps
        // DateTime.Now server-side for translation, and the Sqlite provider can't translate subtracting a
        // TimeSpan parameter from it. A plain DateTime constant translates without issue.
        var expiryCutoff = DateTime.Now - config.Value.CacheTimeToLive;
        return await readContext.Licenses
            .Select(l => new
            {
                Timestamp = EF.Property<DateTime>(l, LicenseDbContext.LastUpdatedPropertyName),
                l.Id
            })
            .Where(obj => obj.Timestamp <= expiryCutoff)
            .Select(obj => obj.Id)
            .ToListAsync(stoppingToken);
    }

    /// <summary>
    /// Removes the stale license and refetches/reinserts it from the OSI API, using its own short-lived
    /// context so each refresh is isolated from the others and from whatever read produced its id.
    /// </summary>
    internal async Task RefreshLicenseAsync(string osiId, CancellationToken stoppingToken)
    {
        await using var writeContext = await dbContextFactory.CreateDbContextAsync(stoppingToken);

        try
        {
            writeContext.Licenses.Remove(new OsiLicense { Id = osiId });
            await writeContext.SaveChangesAsync(stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while removing License \"{OsiId}\".", osiId);
            return;
        }

        try
        {
            var refreshedLicense = await osiClient.GetByOsiIdAsync(osiId, stoppingToken);
            if (refreshedLicense is null)
            {
                logger.LogWarning("License {LicenseId} could not be refetched from OSI API", osiId);
                return;
            }
            await writeContext.Licenses.AddAsync(refreshedLicense, stoppingToken);
            await writeContext.SaveChangesAsync(stoppingToken);
            logger.LogInformation("Refreshed license {LicenseSpdxId}", refreshedLicense.SpdxId ?? refreshedLicense.Id);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while refreshing license {LicenseId}", osiId);
        }
    }

    internal async Task UpdatePeriodicTimerTick(CancellationToken stoppingToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(stoppingToken);
        var oldestTimestamp = await dbContext.Licenses
            .Select(l => EF.Property<DateTime>(l, LicenseDbContext.LastUpdatedPropertyName))
            .MinAsync(stoppingToken);
        var remaining = config.Value.CacheTimeToLive - (DateTime.Now - oldestTimestamp);
        _periodicTimer.Period = remaining > TimeSpan.Zero ? remaining : TimeSpan.FromMilliseconds(1);
    }
}
