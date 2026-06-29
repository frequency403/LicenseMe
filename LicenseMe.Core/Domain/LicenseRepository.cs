using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LicenseMe.Core.Cache;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Core.Domain;

internal sealed class LicenseRepository(
    ILogger<LicenseRepository> logger,
    IOsiClient osiClient,
    ILicenseCacheStore cacheStore,
    LicenseCacheOptions cacheOptions)   
    : ILicenseRepository, ILicenseCollectionWriter
{
    public ObservableCollection<OsiLicense> Licenses { get; } = [];

    void ILicenseCollectionWriter.SetLicenses(IReadOnlyCollection<OsiLicense> value)
    {
        Licenses.Clear();
        foreach (var license in value)
        {
            Licenses.Add(license);
        }
    }

    public void SetLicense(OsiLicense license, bool forceOverride = false)
    {
        if(!forceOverride && Licenses.Contains(license))
            return;
        Licenses.Add(license);
    }

    public bool IsEmpty => Licenses.Count == 0;
    public int CurrentCount => Licenses.Count;
    public int TotalCount => cacheStore.GetCountAsync().GetAwaiter().GetResult();

    private async Task<bool> IsCacheEnabledAndPopulatedAsync(CancellationToken token = default) 
        => cacheOptions.Enabled && await cacheStore.IsPopulatedAsync(token);

    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable([EnumeratorCancellation] CancellationToken token = default)
    {
        var asyncEnumerable = !(await IsCacheEnabledAndPopulatedAsync(token)) 
            ? osiClient.GetAllLicensesAsyncEnumerable(token)
            : cacheStore.GetAllAsync(token);
        await foreach (var license in asyncEnumerable.WithCancellation(token))
        {
            yield return license;
        }
    }

    public async Task<OsiLicense?> GetByOsiIdAsync(string id, CancellationToken token = default)
    {
        if (!(await IsCacheEnabledAndPopulatedAsync(token)))
            return await osiClient.GetByOsiIdAsync(id, token);

        var cached = await cacheStore.GetByOsiIdAsync(id, token);
        if (cached is not null)
            return cached;

        logger.LogDebug("Cache miss for OSI id '{Id}' — falling back to API", id);
        return await osiClient.GetByOsiIdAsync(id, token);
    }

    public async Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id, CancellationToken token = default)
    {
        if (!(await IsCacheEnabledAndPopulatedAsync(token)))
            return await osiClient.GetBySpdxIdAsync(id, token);

        var cached = (await cacheStore.GetBySpdxIdAsync(id, token)).ToList();
        if (cached.Count > 0)
            return cached;

        return await osiClient.GetBySpdxIdAsync(id, token);
    }

    public async Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken token = default)
    {
        if (!(await IsCacheEnabledAndPopulatedAsync(token)))
            return await osiClient.GetByNameAsync(name, token);

        var cached = (await cacheStore.GetByNameAsync(name, token)).ToList();
        if (cached.Count > 0)
            return cached;

        return await osiClient.GetByNameAsync(name, token);
    }

    public async Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword,
        CancellationToken token = default)
    {
        if (!(await IsCacheEnabledAndPopulatedAsync(token)))    
            return await osiClient.GetByKeywordAsync(keyword, token);

        var all = new List<OsiLicense?>();
        await foreach (var license in cacheStore.GetAllAsync(token))
            all.Add(license);

        var filtered = all.Where(l => l?.Keywords?.Contains(keyword) == true).ToList();
        if (filtered.Count > 0)
            return filtered;

        return await osiClient.GetByKeywordAsync(keyword, token);
    }

    public async Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward, CancellationToken token = default)
    {
        if (!(await IsCacheEnabledAndPopulatedAsync(token)))
            return await osiClient.GetByStewardAsync(steward, token);

        var all = new List<OsiLicense?>();
        await foreach (var license in cacheStore.GetAllAsync(token))
            all.Add(license);

        var filtered = all
            .Where(l => l?.Stewards?.Any(s => s.Contains(steward, StringComparison.OrdinalIgnoreCase)) == true)
            .ToList();

        if (filtered.Count > 0)
            return filtered;

        return await osiClient.GetByStewardAsync(steward, token);
    }

    public Task<CacheIntegrity> GetCacheIntegrityAsync(CancellationToken ct = default) =>
        cacheOptions.Enabled
            ? cacheStore.GetIntegrityAsync(ct)
            : Task.FromResult(new CacheIntegrity(IsValid: false, IsExpired: true));

    public void Dispose() => osiClient.Dispose();
    public ValueTask DisposeAsync() => osiClient.DisposeAsync();
}