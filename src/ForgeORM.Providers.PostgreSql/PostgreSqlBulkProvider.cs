using System.Data.Common;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Providers.PostgreSql;

/// <summary>
/// Provider-native bulk provider for PostgreSql. Public ForgeDb bulk APIs stay generic;
/// this class owns the database-specific strategy.
/// </summary>
public sealed class PostgreSqlBulkProvider : IForgeBulkProvider
{
    public static readonly PostgreSqlBulkProvider Instance = new();

    private PostgreSqlBulkProvider()
    {
    }

    public string ProviderName => "PostgreSql";

    public async ValueTask<int> InsertBulkAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken = default)
    {
        await PostgreSqlNativeBulk.BulkInsertAsync(connection, tableName, rows, options, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public async ValueTask<int> UpdateBulkAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn = "Id",
        ForgeProviderBulkOptions? bulkOptions = null,
        CancellationToken cancellationToken = default)
    {
        await PostgreSqlNativeBulk.BulkUpdateAsync(connection, tableName, rows, keyColumn, bulkOptions ?? ForgeProviderBulkOptionsDefaults.Current, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public async ValueTask<int> DeleteBulkAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<TKey> keys,
        string keyColumn = "Id",
        CancellationToken cancellationToken = default)
    {
        await PostgreSqlNativeBulk.BulkDeleteAsync(connection, tableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
        return keys.Count;
    }

    public ValueTask<int> GraphUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn = "Id",
        CancellationToken cancellationToken = default)
        => UpdateBulkAsync(connection, tableName, rows, keyColumn, null, cancellationToken);
}
