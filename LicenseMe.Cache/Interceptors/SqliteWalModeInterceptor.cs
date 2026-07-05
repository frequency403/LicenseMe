using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LicenseMe.Cache.Interceptors;

/// <summary>
/// Switches every opened connection to WAL journaling, so a read (e.g. the UI querying licenses) doesn't
/// block a write (e.g. LicenseFetcher refreshing an expired one) happening at the same time through a
/// different connection - the rollback-journal default takes an exclusive lock for writes while any read
/// cursor is still open elsewhere. journal_mode is persisted in the db file itself, so this is a no-op
/// once already set.
/// </summary>
public sealed class SqliteWalModeInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        await command.ExecuteNonQueryAsync(cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }
}
