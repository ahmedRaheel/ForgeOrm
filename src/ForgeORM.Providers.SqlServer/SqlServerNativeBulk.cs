using System.Data.Common;
using ForgeORM.Core;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

/// <summary>
/// SQL Server native bulk entry point used by the provider.
/// This compile-safe implementation delegates to the shared batched fallback until the
/// strategy-specific TVP implementations are wired into the existing project model.
/// </summary>
internal static class SqlServerNativeBulk
{
    public static ValueTask<int> BulkInsertAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        return ForgeProviderBulkFallback.InsertRowsAsync(connection, tableName, rows, cancellationToken);
    }

    public static ValueTask<int> BulkUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        return ForgeProviderBulkFallback.UpdateRowsAsync(connection, tableName, rows, keyColumn, cancellationToken);
    }

    public static ValueTask<int> BulkDeleteAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        return ForgeProviderBulkFallback.DeleteRowsAsync(connection, tableName, keys, keyColumn, cancellationToken);
    }

    public static ValueTask<int> BulkInsertAsync<T>(
        SqlConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
        => BulkInsertAsync((DbConnection)connection, tableName, rows, cancellationToken);

    public static ValueTask<int> BulkUpdateAsync<T>(
        SqlConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
        => BulkUpdateAsync((DbConnection)connection, tableName, rows, keyColumn, cancellationToken);

    public static ValueTask<int> BulkDeleteAsync<TKey>(
        SqlConnection connection,
        string tableName,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
        => BulkDeleteAsync((DbConnection)connection, tableName, keys, keyColumn, cancellationToken);
}
