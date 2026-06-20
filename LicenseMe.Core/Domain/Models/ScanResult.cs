namespace LicenseMe.Core.Domain.Models;

public sealed record ScanResult(
    string RootPath,
    IReadOnlyList<DiscoveredRepository> Repositories,
    DateTimeOffset ScannedAt);
