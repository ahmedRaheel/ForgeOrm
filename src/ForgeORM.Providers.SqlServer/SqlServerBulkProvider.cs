using ForgeORM.Abstractions;
using ForgeORM.Core;
using System.Data.Common;

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
        ForgeProviderBulkOptions optons,
        CancellationToken cancellationToken = default)
    {
        await SqlServerNativeBulk.BulkInsertAsync(connection, tableName, rows, optons, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public async ValueTask<int> UpdateBulkAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn = "Id",
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await SqlServerNativeBulk.BulkUpdateAsync(connection, tableName, rows, keyColumn, options ?? ForgeProviderBulkOptionsDefaults.Current, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public async ValueTask<int> DeleteBulkAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<TKey> keys,
        string keyColumn = "Id",
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await SqlServerNativeBulk.BulkDeleteAsync(connection, tableName, keys, keyColumn, options ?? ForgeProviderBulkOptionsDefaults.Current, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask<int> GraphUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn = "Id",
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
        => UpdateBulkAsync(connection, tableName, rows, keyColumn, options, cancellationToken);
}
