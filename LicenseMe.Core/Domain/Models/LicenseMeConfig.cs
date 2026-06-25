namespace LicenseMe.Core.Domain.Models;

public sealed record LicenseMeConfig
{
    public bool CachingEnabled { get; init; } = true;
    public string? DefaultLicenseSpdxId { get; init; }
    public List<string> ExcludedPaths { get; init; } = [];
    public int? MaxScanDepth { get; init; }
    public string? LastScanRoot { get; init; }
    public long LastCacheRefresh { get; set; } = 0;

    public static readonly LicenseMeConfig Default = new();
}
