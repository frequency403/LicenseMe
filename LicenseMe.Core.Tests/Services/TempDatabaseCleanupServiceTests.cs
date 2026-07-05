using LicenseMe.Cache.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace LicenseMe.Core.Tests.Services;

public sealed class TempDatabaseCleanupServiceTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"licenseme-cleanup-tests-{Guid.NewGuid():N}.db");

    public void Dispose()
    {
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(_dbPath)}*"))
            File.Delete(file);
    }

    [Fact]
    public async Task StopAsync_DeletesDatabaseFileAndSidecarFiles()
    {
        File.WriteAllText(_dbPath, "db");
        File.WriteAllText(_dbPath + "-wal", "wal");
        File.WriteAllText(_dbPath + "-shm", "shm");
        var sut = new TempDatabaseCleanupService(_dbPath, NullLogger<TempDatabaseCleanupService>.Instance);

        await sut.StopAsync(CancellationToken.None);

        File.Exists(_dbPath).ShouldBeFalse();
        File.Exists(_dbPath + "-wal").ShouldBeFalse();
        File.Exists(_dbPath + "-shm").ShouldBeFalse();
    }

    [Fact]
    public async Task StopAsync_FileDoesNotExist_DoesNotThrow()
    {
        var sut = new TempDatabaseCleanupService(_dbPath, NullLogger<TempDatabaseCleanupService>.Instance);

        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));
    }
}
