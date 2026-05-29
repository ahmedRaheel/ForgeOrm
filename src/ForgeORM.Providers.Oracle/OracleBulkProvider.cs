using System.Data.Common;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Providers.Oracle;

/// <summary>
/// Provider-native bulk provider for Oracle. Public ForgeDb bulk APIs stay generic;
/// this class owns the database-specific strategy.
/// </summary>
public sealed class OracleBulkProvider : IForgeBulkProvider
{
    public static readonly OracleBulkProvider Instance = new();

    private OracleBulkProvider()
    {
    }

    public string ProviderName => "Oracle";

    public async ValueTask<int> InsertBulkAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken = default)
    {
        await OracleNativeBulk.BulkInsertAsync(connection, tableName, rows, options, cancellationToken).ConfigureAwait(false);
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
        await OracleNativeBulk.BulkUpdateAsync(connection, tableName, rows, keyColumn, bulkOptions ?? ForgeProviderBulkOptionsDefaults.Current, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public async ValueTask<int> DeleteBulkAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<TKey> keys,
        string keyColumn = "Id",
        CancellationToken cancellationToken = default)
    {
        await OracleNativeBulk.BulkDeleteAsync(connection, tableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
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
