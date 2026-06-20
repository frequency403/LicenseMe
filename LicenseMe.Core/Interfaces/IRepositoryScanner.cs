using LicenseMe.Core.Domain.Models;

namespace LicenseMe.Core.Interfaces;

public interface IRepositoryScanner
{
    IAsyncEnumerable<DiscoveredRepository> ScanAsync(
        string rootPath,
        CancellationToken ct = default);
}
