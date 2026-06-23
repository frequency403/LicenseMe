using System.Data;
using Dapper;
using LicenseMe.Core.Cache;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Distributed;

public sealed class SqliteDistributedCache : IDistributedCache, IAsyncDisposable
{
    private const string PragmaJournalModeWal = "PRAGMA journal_mode=WAL;";

    private const string Sql = """
                               CREATE TABLE IF NOT EXISTS cache_entries (
                                   key          TEXT PRIMARY KEY,
                                   value        BLOB NOT NULL,
                                   expires_at   INTEGER,
                                   sliding_secs INTEGER
                               );
                               """;

    private const string SelectSql = "SELECT value, expires_at AS ExpiresAt, sliding_secs AS SlidingSecs FROM cache_entries WHERE key = @key";

    private const string SetSql = """
                                  INSERT INTO cache_entries (key, value, expires_at, sliding_secs)
                                  VALUES (@key, @value, @expiresAt, @slidingSec)
                                  ON CONFLICT(key) DO UPDATE
                                      SET value        = excluded.value,
                                          expires_at   = excluded.expires_at,
                                          sliding_secs = excluded.sliding_secs;
                                  """;

    private const string SelectSlidingSecsFromCacheEntriesWhereKeyKey = "SELECT sliding_secs FROM cache_entries WHERE key = @key";
    private const string DeleteFromCacheEntriesWhereKeyKey = "DELETE FROM cache_entries WHERE key = @key";
    private const string UpdateCacheEntriesSetExpiresAtNewexpiryWhereKeyKey = "UPDATE cache_entries SET expires_at = @newExpiry WHERE key = @key";

    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SqliteDistributedCache(SqliteCacheOptions options)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = options.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
            Cache = SqliteCacheMode.Private
        };


        _connection = new SqliteConnection(connectionStringBuilder.ToString());
        _connection.Open();
        if(options.EnableWalMode)
            _connection.Execute(PragmaJournalModeWal);
        InitializeSchema();
    }

    private void InitializeSchema()
    {
        _connection.Execute(Sql);
    }

    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var row = await _connection.QuerySingleOrDefaultAsync<CacheRow>(
                SelectSql,
                new { key });

            if (row is null)
                return null;

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (row.ExpiresAt.HasValue && row.ExpiresAt.Value < now)
            {
                await RemoveInternalAsync(key);
                return null;
            }

            if (row.SlidingSecs.HasValue)
                await TouchAsync(key, row.SlidingSecs.Value);

            return row.Value;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => SetAsync(key, value, options).GetAwaiter().GetResult();

    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        var now        = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiresAt  = ResolveExpiry(options, now);
        var slidingSec = (long?)options.SlidingExpiration?.TotalSeconds;

        await _semaphore.WaitAsync(token);
        try
        {
            await _connection.ExecuteAsync(SetSql,
                new { key, value, expiresAt, slidingSec });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token);
        try
        {
            var slidingSecs = await _connection.ExecuteScalarAsync<long?>(
                SelectSlidingSecsFromCacheEntriesWhereKeyKey,
                new { key });

            if (slidingSecs.HasValue)
                await TouchAsync(key, slidingSecs.Value);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        await _semaphore.WaitAsync(token);
        try { await RemoveInternalAsync(key); }
        finally { _semaphore.Release(); }
    }

    private async Task TouchAsync(string key, long slidingSec)
    {
        var newExpiry = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + slidingSec;
        await _connection.ExecuteAsync(
            UpdateCacheEntriesSetExpiresAtNewexpiryWhereKeyKey,
            new { newExpiry, key });
    }

    private Task RemoveInternalAsync(string key) =>
        _connection.ExecuteAsync(DeleteFromCacheEntriesWhereKeyKey, new { key });

    private static long? ResolveExpiry(DistributedCacheEntryOptions options, long now) =>
        options switch
        {
            { AbsoluteExpiration: { } abs }            => abs.ToUnixTimeSeconds(),
            { AbsoluteExpirationRelativeToNow: { } r } => now + (long)r.TotalSeconds,
            { SlidingExpiration: { } s }               => now + (long)s.TotalSeconds,
            _                                          => null
        };

    public async ValueTask DisposeAsync()
    {
        _semaphore.Dispose();
        await _connection.DisposeAsync();
    }

    private sealed record CacheRow(byte[] Value, long? ExpiresAt, long? SlidingSecs);
}