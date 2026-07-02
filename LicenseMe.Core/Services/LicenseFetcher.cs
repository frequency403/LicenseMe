using LicenseMe.Cache.Context;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSourceInitiative.LicenseApi.Interfaces;

namespace LicenseMe.Core.Services;

public class LicenseFetcher(ILogger<LicenseFetcher> logger, IOsiClient osiClient, IOptions<LicenseMeConfig> config, [FromKeyedServices("LicenseReporter")] IProgressReporter<string> progressReporter, IServiceProvider serviceProvider) : BackgroundService
{
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
                    await using var scope = serviceProvider.CreateAsyncScope();
                    await using var dbContext = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
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
        await using var scope = serviceProvider.CreateAsyncScope();
        await using (var dbContext = scope.ServiceProvider.GetRequiredService<LicenseDbContext>())
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
        
        while (!stoppingToken.IsCancellationRequested)
        {
            
        }
    }
}