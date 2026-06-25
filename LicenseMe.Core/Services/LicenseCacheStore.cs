using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Dapper;
using LicenseMe.Core.Cache;
using LicenseMe.Core.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OpenSourceInitiative.LicenseApi.Models;

internal sealed class LicenseCacheStore : ILicenseCacheStore
{
    // ── DDL ───────────────────────────────────────────────────────────────────
    private const string CreateTableSql =
        """
        CREATE TABLE IF NOT EXISTS license_cache (
            spdx_id      TEXT    PRIMARY KEY NOT NULL,
            osi_id       TEXT    NOT NULL,
            name         TEXT    NOT NULL,
            payload      TEXT    NOT NULL,
            license_text TEXT    NOT NULL DEFAULT '',
            cached_at    INTEGER NOT NULL
        )
        """;

    private const string CreateOsiIdIndexSql =
        "CREATE INDEX IF NOT EXISTS idx_osi_id ON license_cache(osi_id)";

    // ── DML ───────────────────────────────────────────────────────────────────
    private const string UpsertSql =
        """
        INSERT OR REPLACE INTO license_cache (spdx_id, osi_id, name, payload, license_text, cached_at)
        VALUES (@SpdxId, @OsiId, @Name, @Payload, @LicenseText, @CachedAt)
        """;

    private const string SelectBySpdxIdSql =
        """
        SELECT payload      AS Payload,
               license_text AS LicenseText,
               cached_at    AS CachedAt
        FROM   license_cache
        WHERE  spdx_id LIKE @Pattern AND cached_at >= @Threshold
        """;

    private const string SelectByOsiIdSql =
        """
        SELECT payload      AS Payload,
               license_text AS LicenseText,
               cached_at    AS CachedAt
        FROM   license_cache
        WHERE  osi_id = @OsiId AND cached_at >= @Threshold
        """;

    private const string SelectByNameSql =
        """
        SELECT payload      AS Payload,
               license_text AS LicenseText,
               cached_at    AS CachedAt
        FROM   license_cache
        WHERE  name LIKE '%' || @Name || '%' AND cached_at >= @Threshold
        """;

    private const string SelectAllSql =
        """
        SELECT payload      AS Payload,
               license_text AS LicenseText,
               cached_at    AS CachedAt
        FROM   license_cache
        WHERE  cached_at >= @Threshold
        """;

    private const string DeleteExpiredSql =
        "DELETE FROM license_cache WHERE cached_at < @Threshold";

    // ── Integrity ─────────────────────────────────────────────────────────────
    private const string CountTotalSql =
        "SELECT COUNT(*) FROM license_cache";

    private const string CountExpiredSql =
        "SELECT COUNT(*) FROM license_cache WHERE cached_at < @Threshold";

    private const string SelectAllPayloadsSql =
        """
        SELECT payload      AS Payload,
               license_text AS LicenseText,
               cached_at    AS CachedAt
        FROM   license_cache
        """;

    private const string CountTotalNonExpiredSql =
        "SELECT COUNT(*) FROM license_cache WHERE cached_at >= @Threshold";

    private const string LogSchemaInit = "Initializing license cache schema at '{0}'";
    private const string LogPurged     = "Purged {0} expired entries from license cache";

    private static readonly PropertyInfo LicenseTextProperty =
        typeof(OsiLicense).GetProperty(
            nameof(OsiLicense.LicenseText),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;


    private sealed record CacheRow(string Payload, string LicenseText, long CachedAt);

    private readonly ILogger<LicenseCacheStore> _logger;
    private readonly LicenseCacheOptions _options;
    private readonly string _connectionString;

    public LicenseCacheStore(ILogger<LicenseCacheStore> logger, LicenseCacheOptions options)
    {
        this._logger           = logger;
        this._options          = options;
        this._connectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = options.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true
        }.ToString();

        if (this._options.Enabled)
            InitializeSchema();
    }

    private void InitializeSchema()
    {
        _logger.LogInformation(string.Format(LogSchemaInit, _options.DatabasePath));
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute(CreateTableSql);
        connection.Execute(CreateOsiIdIndexSql);
    }


    public async Task UpsertAsync(OsiLicense license, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(UpsertSql, BuildRow(license));
    }

    public async Task UpsertBulkAsync(IEnumerable<OsiLicense> licenses, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        try
        {
            foreach (var license in licenses)
                await connection.ExecuteAsync(UpsertSql, BuildRow(license), transaction);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IEnumerable<OsiLicense?>> GetBySpdxIdAsync(string spdxId, CancellationToken ct = default)
    {
        var pattern = spdxId.Replace("*", "%");
        await using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<CacheRow>(
            SelectBySpdxIdSql, new { Pattern = pattern, Threshold = Threshold() });

        return rows.Select(Deserialize);
    }

    public async Task<OsiLicense?> GetByOsiIdAsync(string osiId, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var row = await connection.QueryFirstOrDefaultAsync<CacheRow>(
            SelectByOsiIdSql, new { OsiId = osiId, Threshold = Threshold() });

        return row is null ? null : Deserialize(row);
    }

    public async Task<IEnumerable<OsiLicense?>> GetByNameAsync(string name, CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<CacheRow>(
            SelectByNameSql, new { Name = name, Threshold = Threshold() });

        return rows.Select(Deserialize);
    }

    public async IAsyncEnumerable<OsiLicense> GetAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<CacheRow>(
            SelectAllSql, new { Threshold = Threshold() });

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            var license = Deserialize(row);
            if (license is not null)
                yield return license;
        }
    }

    public async Task PurgeExpiredAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var count = await connection.ExecuteAsync(DeleteExpiredSql, new { Threshold = Threshold() });
        _logger.LogInformation(string.Format(LogPurged, count));
    }

    public async Task<bool> IsPopulatedAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            CountTotalNonExpiredSql, new { Threshold = Threshold() });
        return count > 0;
    }

    public async Task<CacheIntegrity> GetIntegrityAsync(CancellationToken ct = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var total = await connection.ExecuteScalarAsync<int>(CountTotalSql);
        if (total == 0)
            return new CacheIntegrity(IsValid: false, IsExpired: true);

        var expiredCount = await connection.ExecuteScalarAsync<int>(
            CountExpiredSql, new { Threshold = Threshold() });

        var rows    = await connection.QueryAsync<CacheRow>(SelectAllPayloadsSql);
        var isValid = rows.All(r =>
        {
            var license = Deserialize(r);
            return license is not null
                && !string.IsNullOrWhiteSpace(license.Id)
                && !string.IsNullOrWhiteSpace(license.Name)
                && !string.IsNullOrWhiteSpace(license.LicenseText);
        });

        return new CacheIntegrity(
            IsValid:   isValid,
            IsExpired: expiredCount == total);
    }


    private long Threshold() =>
        DateTimeOffset.UtcNow.Subtract(_options.Ttl).ToUnixTimeSeconds();

    private static object BuildRow(OsiLicense license) => new
    {
        SpdxId      = license.SpdxId,
        OsiId       = license.Id,
        Name        = license.Name,
        Payload     = JsonSerializer.Serialize(license),
        LicenseText = license.LicenseText,
        CachedAt    = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
    };

    private static OsiLicense? Deserialize(CacheRow row)
    {
        var license = JsonSerializer.Deserialize<OsiLicense>(row.Payload);
        if (license is null)
            return null;

        LicenseTextProperty.SetValue(license, row.LicenseText);
        return license;
    }
}