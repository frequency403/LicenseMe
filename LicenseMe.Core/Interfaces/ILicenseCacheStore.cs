using LicenseMe.Core.Cache;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Core.Interfaces;

internal interface ILicenseCacheStore
{
    Task UpsertAsync(OsiLicense license, CancellationToken ct = default);
    Task UpsertBulkAsync(IEnumerable<OsiLicense> licenses, CancellationToken ct = default);
    Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string spdxId, CancellationToken ct = default);
    Task<OsiLicense?> GetByOsiIdAsync(string osiId, CancellationToken ct = default);
    Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken ct = default);
    IAsyncEnumerable<OsiLicense> GetAllAsync(CancellationToken ct = default);
    Task PurgeExpiredAsync(CancellationToken ct = default);
    Task<CacheIntegrity> GetIntegrityAsync(CancellationToken ct = default);
    Task<bool> IsPopulatedAsync(CancellationToken ct = default);
}