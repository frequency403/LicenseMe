using LicenseMe.Core.Cache;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Core.Interfaces;

public interface ILicenseRepository
{
    IReadOnlyCollection<OsiLicense> Licenses { get; }
    IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable(CancellationToken token = default);
    Task<OsiLicense?> GetByOsiIdAsync(string id, CancellationToken token = default);
    Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id, CancellationToken token = default);
    Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken token = default);
    Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword, CancellationToken token = default);
    Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward, CancellationToken token = default);
    Task<CacheIntegrity> GetCacheIntegrityAsync(CancellationToken ct = default);
}