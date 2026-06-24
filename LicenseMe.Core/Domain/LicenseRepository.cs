using System.Runtime.CompilerServices;
using LicenseMe.Core.Cache;
using LicenseMe.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Interfaces;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Core.Domain;

internal sealed class LicenseRepository : ILicenseRepository, ILicenseCollectionWriter
{
    private readonly ILogger<LicenseRepository> _logger;
    private readonly IOsiClient _osiClient;
    private readonly ILicenseCacheStore _cacheStore;
    private readonly LicenseCacheOptions _options;
    
    private volatile IReadOnlyCollection<OsiLicense> _licenses = [];
    public IReadOnlyCollection<OsiLicense> Licenses => _licenses;
    
    void ILicenseCollectionWriter.SetLicenses(IReadOnlyCollection<OsiLicense> value) =>
        _licenses = value;

    public LicenseRepository(
        ILogger<LicenseRepository> logger,
        IOsiClient osiClient,
        ILicenseCacheStore cacheStore,
        LicenseCacheOptions cacheOptions)
    {
        this._logger = logger;
        this._osiClient = osiClient;
        this._cacheStore = cacheStore;
        this._options = cacheOptions;
    }

    public async IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable([EnumeratorCancellation] CancellationToken token = default)
    {
        var asyncEnumerable = !_options.Enabled || !(await _cacheStore.IsPopulatedAsync(token)) 
            ? _osiClient.GetAllLicensesAsyncEnumerable(token)
            : _cacheStore.GetAllAsync(token);
        await foreach (var license in asyncEnumerable.WithCancellation(token))
        {
            yield return license;
        }
    }

    public async Task<OsiLicense?> GetByOsiIdAsync(string id, CancellationToken token = default)
    {
        if (!_options.Enabled || !await _cacheStore.IsPopulatedAsync(token))
            return await _osiClient.GetByOsiIdAsync(id, token);

        var cached = await _cacheStore.GetByOsiIdAsync(id, token);
        if (cached is not null)
            return cached;

        _logger.LogDebug("Cache miss for OSI id '{Id}' — falling back to API", id);
        return await _osiClient.GetByOsiIdAsync(id, token);
    }

    public async Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id, CancellationToken token = default)
    {
        if (!_options.Enabled || !await _cacheStore.IsPopulatedAsync(token))
            return await _osiClient.GetBySpdxIdAsync(id, token);

        var cached = (await _cacheStore.GetBySpdxIdAsync(id, token)).ToList();
        if (cached.Count > 0)
            return cached;

        return await _osiClient.GetBySpdxIdAsync(id, token);
    }

    public async Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken token = default)
    {
        if (!_options.Enabled || !await _cacheStore.IsPopulatedAsync(token))
            return await _osiClient.GetByNameAsync(name, token);

        var cached = (await _cacheStore.GetByNameAsync(name, token)).ToList();
        if (cached.Count > 0)
            return cached;

        return await _osiClient.GetByNameAsync(name, token);
    }

    public async Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword,
        CancellationToken token = default)
    {
        if (!_options.Enabled || !await _cacheStore.IsPopulatedAsync(token))
            return await _osiClient.GetByKeywordAsync(keyword, token);

        var all = new List<OsiLicense?>();
        await foreach (var license in _cacheStore.GetAllAsync(token))
            all.Add(license);

        var filtered = all.Where(l => l?.Keywords?.Contains(keyword) == true).ToList();
        if (filtered.Count > 0)
            return filtered;

        return await _osiClient.GetByKeywordAsync(keyword, token);
    }

    public async Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward, CancellationToken token = default)
    {
        if (!_options.Enabled || !await _cacheStore.IsPopulatedAsync(token))
            return await _osiClient.GetByStewardAsync(steward, token);

        var all = new List<OsiLicense?>();
        await foreach (var license in _cacheStore.GetAllAsync(token))
            all.Add(license);

        var filtered = all
            .Where(l => l?.Stewards?.Any(s => s.Contains(steward, StringComparison.OrdinalIgnoreCase)) == true)
            .ToList();

        if (filtered.Count > 0)
            return filtered;

        return await _osiClient.GetByStewardAsync(steward, token);
    }

    public Task<CacheIntegrity> GetCacheIntegrityAsync(CancellationToken ct = default) =>
        _options.Enabled
            ? _cacheStore.GetIntegrityAsync(ct)
            : Task.FromResult(new CacheIntegrity(IsValid: false, IsExpired: true));

    public void Dispose() => _osiClient.Dispose();
    public ValueTask DisposeAsync() => _osiClient.DisposeAsync();
}