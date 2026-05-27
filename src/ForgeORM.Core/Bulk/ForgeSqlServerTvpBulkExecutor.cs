using ForgeORM.Core.Graph;
using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Compatibility facade kept for older internal call sites. SQL Server-specific TVP/SqlDataRecord
/// execution now lives in the SQL Server provider project. Core remains provider-neutral.
/// </summary>
internal static class ForgeSqlServerTvpBulkExecutor
{
    public static ValueTask<int> InsertAsync<T>(
        DbConnection connection,
        ForgeEntityMetadata metadata,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        var list = rows as IReadOnlyList<T> ?? rows.ToArray();
        return ForgeProviderBulkFallback.InsertRowsAsync(connection, metadata.TableName, list, cancellationToken);
    }

    public static ValueTask<int> InsertAsync<T>(
        DbConnection connection,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        var metadata = ForgeEntityMetadataCache.Get(typeof(T));
        return ForgeProviderBulkFallback.InsertRowsAsync(connection, metadata.TableName, rows, cancellationToken);
    }

    public static ValueTask<int> UpdateAsync<T>(
        DbConnection connection,
        ForgeEntityMetadata metadata,
        IReadOnlyCollection<T> rows,
        string? keyColumn = null,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        var list = rows as IReadOnlyList<T> ?? rows.ToArray();
        var key = keyColumn ?? metadata.KeyProperty?.Name ?? "Id";

        return ForgeProviderBulkFallback.UpdateRowsAsync(
            connection,
            metadata.TableName,
            list,
            key,
            cancellationToken);
    }

    public static ValueTask<int> DeleteAsync<TKey>(
        DbConnection connection,
        string tableName,
        string keyColumn,
        IReadOnlyCollection<TKey> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return ValueTask.FromResult(0);

        var list = ids as IReadOnlyList<TKey> ?? ids.ToArray();

        return ForgeProviderBulkFallback.DeleteRowsAsync(
            connection,
            tableName,
            list,
            keyColumn,
            cancellationToken);
    }

    public static ValueTask<int> UpdateObjectsAsync(
        DbConnection connection,
        DbTransaction? transaction,
        ForgeEntityMetadata metadata,
        IReadOnlyCollection<object> rows,
        string? keyColumn = null,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        var list = rows as IReadOnlyList<object> ?? rows.ToArray();
        var key = keyColumn ?? metadata.KeyProperty?.Name ?? "Id";

        return ForgeProviderBulkFallback.UpdateRowsAsync(
            connection,
            metadata.TableName,
            list,
            key,
            cancellationToken);
    }
}
