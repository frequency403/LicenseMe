using System.Collections.ObjectModel;
using LicenseMe.Core.Cache;
using OpenSourceInitiative.LicenseApi.Enums;
using OpenSourceInitiative.LicenseApi.Models;

namespace LicenseMe.Core.Interfaces;

public interface ILicenseRepository : IDisposable, IAsyncDisposable
{
    ObservableCollection<OsiLicense> Licenses { get; }
    bool IsEmpty { get; }
    int CurrentCount { get; }
    int TotalCount { get; }
    IAsyncEnumerable<OsiLicense?> GetAllLicensesAsyncEnumerable(CancellationToken token = default);
    Task<OsiLicense?> GetByOsiIdAsync(string id, CancellationToken token = default);
    Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string id, CancellationToken token = default);
    Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken token = default);
    Task<IEnumerable<OsiLicense?>> GetByKeywordAsync(OsiLicenseKeyword keyword, CancellationToken token = default);
    Task<IEnumerable<OsiLicense?>> GetByStewardAsync(string steward, CancellationToken token = default);
    Task<CacheIntegrity> GetCacheIntegrityAsync(CancellationToken ct = default);
}