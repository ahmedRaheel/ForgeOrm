using System.Data.Common;
using ForgeORM.Core;
using ForgeORM.Core.Bulk;

namespace ForgeORM.Providers.PostgreSql;

internal static class PostgreSqlNativeBulk
{
    public static async ValueTask<int> BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0) return 0;
        // Default PostgreSQL strategy: COPY. Current safe implementation uses provider-neutral batched insert until COPY writer is plugged in.
        return await ForgeProviderBulkFallback.InsertRowsAsync(connection, tableName, rows as IReadOnlyList<T> ?? rows.ToArray(), cancellationToken).ConfigureAwait(false);
    }
    public static async ValueTask<int> BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0) return 0;
        // Default PostgreSQL strategy: temp table + UPDATE FROM. Safe implementation uses batched update.
        return await ForgeProviderBulkFallback.UpdateRowsAsync(connection, tableName, rows as IReadOnlyList<T> ?? rows.ToArray(), keyColumn, cancellationToken).ConfigureAwait(false);
    }
    public static async ValueTask<int> BulkDeleteAsync<TKey>(DbConnection connection, string tableName, IReadOnlyCollection<TKey> keys, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (keys is null || keys.Count == 0) return 0;
        return await ForgeProviderBulkFallback.DeleteRowsAsync(connection, tableName, keys as IReadOnlyList<TKey> ?? keys.ToArray(), keyColumn, cancellationToken).ConfigureAwait(false);
    }
}
