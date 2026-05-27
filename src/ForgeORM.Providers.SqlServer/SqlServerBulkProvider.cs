using System.Data.Common;
using ForgeORM.Core;

namespace ForgeORM.Providers.SqlServer;

/// <summary>
/// Provider-native bulk provider for SqlServer. Public ForgeDb bulk APIs stay generic;
/// this class owns the database-specific strategy.
/// </summary>
public sealed class SqlServerBulkProvider : IForgeBulkProvider
{
    public static readonly SqlServerBulkProvider Instance = new();

    private SqlServerBulkProvider()
    {
    }

    public string ProviderName => "SqlServer";

    public async ValueTask<int> InsertBulkAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        await SqlServerNativeBulk.BulkInsertAsync(connection, tableName, rows, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public async ValueTask<int> UpdateBulkAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn = "Id",
        CancellationToken cancellationToken = default)
    {
        await SqlServerNativeBulk.BulkUpdateAsync(connection, tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public async ValueTask<int> DeleteBulkAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<TKey> keys,
        string keyColumn = "Id",
        CancellationToken cancellationToken = default)
    {
        // Provider-native delete can be specialized per database:
        // SQL Server: key TVP + DELETE JOIN.
        // PostgreSQL: temp key table + DELETE USING.
        // MySQL: temp key table + DELETE JOIN.
        // Oracle: array-bound DELETE.
        await BulkFallback.DeleteAsync(connection, tableName, keys.Cast<object>().ToArray(), keyColumn, cancellationToken).ConfigureAwait(false);
        return keys.Count;
    }

    public ValueTask<int> GraphUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn = "Id",
        CancellationToken cancellationToken = default)
        => UpdateBulkAsync(connection, tableName, rows, keyColumn, cancellationToken);
}
