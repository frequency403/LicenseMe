using LicenseMe.Cache.Context;
using LicenseMe.Cache.Interceptors;
using LicenseMe.Core.Domain.Models;
using LicenseMe.Core.Interfaces;
using LicenseMe.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;
using Shouldly;
using Xunit;

namespace LicenseMe.Core.Tests.Services;

/// <summary>
/// Exercises LicenseFetcher's tick-refresh logic against a real, file-backed SQLite database (matching
/// what ServiceCollectionExtensions now wires up in both LicenseMeConfig.CachingEnabled cases - a
/// persistent path or an ephemeral temp path, both plain WAL-journaled files, no shared-cache in-memory
/// database involved anymore).
/// </summary>
public sealed class LicenseFetcherRefreshTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"licenseme-tests-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _serviceProvider;
    private readonly IOsiClient _osiClientMock = Substitute.For<IOsiClient>();
    private readonly LicenseMeConfig _config = new();
    private readonly LicenseFetcher _sut;

    public LicenseFetcherRefreshTests()
    {
        var services = new ServiceCollection();
        void ConfigureDbContext(DbContextOptionsBuilder options) => options
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .UseSqlite($"Data Source={_dbPath}", opt => opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .AddInterceptors(new SqliteWalModeInterceptor());
        services.AddDbContext<LicenseDbContext>(ConfigureDbContext);
        services.AddDbContextFactory<LicenseDbContext>(ConfigureDbContext);
        _serviceProvider = services.BuildServiceProvider();

        using (var scope = _serviceProvider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<LicenseDbContext>().Database.Migrate();
        }

        _sut = new LicenseFetcher(
            NullLogger<LicenseFetcher>.Instance,
            _osiClientMock,
            Options.Create(_config),
            Substitute.For<IProgressReporter<string>>(),
            _serviceProvider.GetRequiredService<IDbContextFactory<LicenseDbContext>>());
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(_dbPath)}*"))
            File.Delete(file);
    }

    private static OsiLicense CreateLicense(string id, string? name = null, string? spdxId = null) => new()
    {
        Id = id,
        Name = name ?? id,
        SpdxId = spdxId,
        Links = new OsiLicenseLinks
        {
            Self = new OsiHref { Href = $"https://api.opensource.org/licenses/{id}" },
            Html = new OsiHref { Href = $"https://opensource.org/licenses/{id}" },
            Collection = new OsiHref { Href = "https://api.opensource.org/licenses" }
        }
    };

    /// <summary>
    /// SQLite's CURRENT_TIMESTAMP (used by the LastUpdatedUtc default) only has second-level resolution,
    /// so tests that need a deterministic "how old is this row" story pass <paramref name="lastUpdatedUtc"/>
    /// and it's written directly, bypassing the default/trigger-generated value entirely.
    /// </summary>
    private async Task SeedLicenseAsync(string id, DateTime? lastUpdatedUtc = null)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
        await dbContext.Licenses.AddAsync(CreateLicense(id));
        await dbContext.SaveChangesAsync();

        if (lastUpdatedUtc is { } timestamp)
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE OsiLicenseTimestamp SET Timestamp = {timestamp} WHERE Id = {id}");
        }
    }

    private async Task<OsiLicense?> FindLicenseAsync(string id)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var dbContext = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();
        return await dbContext.Licenses.FirstOrDefaultAsync(l => l.Id == id);
    }

    [Fact]
    public async Task GetExpiredLicenseIdsAsync_ReturnsOnlyLicensesOlderThanTtl()
    {
        await SeedLicenseAsync("old-mit", DateTime.Now.AddDays(-40));
        await SeedLicenseAsync("fresh-apache", DateTime.Now);
        _config.CacheTimeToLive = TimeSpan.FromDays(30);

        await using var scope = _serviceProvider.CreateAsyncScope();
        await using var readContext = scope.ServiceProvider.GetRequiredService<LicenseDbContext>();

        var expiredIds = await _sut.GetExpiredLicenseIdsAsync(readContext, CancellationToken.None);

        expiredIds.ShouldBe(["old-mit"]);
    }

    [Fact]
    public async Task RefreshLicenseAsync_WhenApiReturnsLicense_ReplacesStaleEntity()
    {
        await SeedLicenseAsync("mit");
        _osiClientMock.GetByOsiIdAsync("mit", Arg.Any<CancellationToken>())
            .Returns(CreateLicense("mit", "Refreshed MIT", "MIT"));

        await _sut.RefreshLicenseAsync("mit", CancellationToken.None);

        var refreshed = await FindLicenseAsync("mit");
        refreshed.ShouldNotBeNull();
        refreshed.Name.ShouldBe("Refreshed MIT");
        refreshed.SpdxId.ShouldBe("MIT");
    }

    [Fact]
    public async Task RefreshLicenseAsync_WhenApiReturnsNull_RemovesEntityWithoutReinserting()
    {
        await SeedLicenseAsync("gone");
        _osiClientMock.GetByOsiIdAsync("gone", Arg.Any<CancellationToken>())
            .Returns((OsiLicense?)null);

        await _sut.RefreshLicenseAsync("gone", CancellationToken.None);

        (await FindLicenseAsync("gone")).ShouldBeNull();
    }

    [Fact]
    public async Task UpdatePeriodicTimerTick_ClampsToMinimumWhenOldestLicenseAlreadyExpired()
    {
        await SeedLicenseAsync("mit", DateTime.Now.AddDays(-1));
        _config.CacheTimeToLive = TimeSpan.FromMilliseconds(1);

        await _sut.UpdatePeriodicTimerTick(CancellationToken.None);

        _sut.CurrentPeriod.ShouldBe(TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task UpdatePeriodicTimerTick_SetsPeriodToRemainingTtlOfOldestLicense()
    {
        await SeedLicenseAsync("mit");
        _config.CacheTimeToLive = TimeSpan.FromSeconds(10);

        await _sut.UpdatePeriodicTimerTick(CancellationToken.None);

        _sut.CurrentPeriod.ShouldBeGreaterThan(TimeSpan.Zero);
        _sut.CurrentPeriod.ShouldBeLessThanOrEqualTo(_config.CacheTimeToLive);
    }

    [Fact]
    public async Task FullTick_ReadThenSequentialRefresh_CompletesForAllExpiredLicenses()
    {
        var ids = Enumerable.Range(0, 5).Select(i => $"license-{i}").ToArray();
        foreach (var id in ids)
            await SeedLicenseAsync(id, DateTime.Now.AddDays(-1));
        _config.CacheTimeToLive = TimeSpan.FromHours(1);

        _osiClientMock.GetByOsiIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => CreateLicense(callInfo.Arg<string>(), "Refreshed"));

        // Mirrors ExecuteAsync's tick body: the read context is fully closed before any refresh opens a
        // write.
        List<string> expiredIds;
        await using (var scope = _serviceProvider.CreateAsyncScope())
        await using (var readContext = scope.ServiceProvider.GetRequiredService<LicenseDbContext>())
        {
            expiredIds = await _sut.GetExpiredLicenseIdsAsync(readContext, CancellationToken.None);
        }

        foreach (var id in expiredIds)
            await _sut.RefreshLicenseAsync(id, CancellationToken.None);

        expiredIds.Count.ShouldBe(5);
        foreach (var id in ids)
        {
            var license = await FindLicenseAsync(id);
            license.ShouldNotBeNull();
            license.Name.ShouldBe("Refreshed");
        }
    }
}
