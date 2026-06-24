namespace LicenseMe.Core.Cache;

public sealed class LicenseCacheOptions
{
    public bool Enabled             { get; init; } = true;
    public string DatabasePath      { get; init; } = "license_cache.db";
    public TimeSpan Ttl             { get; init; } = TimeSpan.FromHours(24);
    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromHours(6);
}

public sealed record CacheIntegrity(bool IsValid, bool IsExpired);