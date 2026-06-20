using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Services;
using Shouldly;
using Xunit;

namespace LicenseMe.Core.Tests.Services;

public sealed class ConfigManagerTests : IDisposable
{
    private readonly string _tempConfigPath =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "config.json");
    
    public void Dispose()
    {
        var dir = Path.GetDirectoryName(_tempConfigPath);
        if (dir is not null && Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public async Task LoadAsync_NoFile_ReturnsDefault()
    {
        var sut = new ConfigManager();
        // ConfigPath won't exist in test runner environment (different APPDATA)
        var config = await sut.LoadAsync(TestContext.Current.CancellationToken);
        config.CachingEnabled.ShouldBeTrue();
        config.ExcludedPaths.ShouldBeEmpty();
    }
}
