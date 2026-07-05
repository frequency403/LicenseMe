using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LicenseMe.Cache.Services;

/// <summary>
/// Deletes the ephemeral SQLite database file (and its WAL/SHM/journal sidecar files) used when
/// LicenseMeConfig.CachingEnabled is false, on a graceful app shutdown. This only covers the "the app
/// closed normally" case, since StopAsync never runs on a crash or a forced kill - ConfigManager's
/// temp-directory sweep on the next startup is what actually guarantees these don't accumulate.
/// </summary>
public sealed class TempDatabaseCleanupService(string databasePath, ILogger<TempDatabaseCleanupService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var file in new[] { databasePath, databasePath + "-wal", databasePath + "-shm", databasePath + "-journal" })
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Could not delete temporary database file {File}; next startup's sweep will remove it", file);
            }
        }
        return Task.CompletedTask;
    }
}
