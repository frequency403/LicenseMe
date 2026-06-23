using Microsoft.Extensions.Options;

namespace LicenseMe.Core.Cache;

public sealed class SqliteCacheOptions : IOptions<SqliteCacheOptions>
{
    public string DatabasePath { get; set; } = "cache.db";
    public bool EnableWalMode  { get; set; } = true; 
    SqliteCacheOptions IOptions<SqliteCacheOptions>.Value => this;
}