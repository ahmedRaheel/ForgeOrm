using System.Data.Common;
using ForgeORM.Core;

namespace ForgeORM.Providers.Oracle;

internal static class OracleNativeBulk
{
    public static async ValueTask<int> BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0) return 0;
        // Default Oracle strategy: array binding. Safe implementation uses batched insert until array binding writer is plugged in.
        return await ForgeProviderBulkFallback.InsertRowsAsync(connection, tableName, rows as IReadOnlyList<T> ?? rows.ToArray(), cancellationToken).ConfigureAwait(false);
    }
    public static async ValueTask<int> BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0) return 0;
        // Default Oracle strategy: OracleMerge/array binding. Safe implementation uses batched update.
        return await ForgeProviderBulkFallback.UpdateRowsAsync(connection, tableName, rows as IReadOnlyList<T> ?? rows.ToArray(), keyColumn, cancellationToken).ConfigureAwait(false);
    }
    public static async ValueTask<int> BulkDeleteAsync<TKey>(DbConnection connection, string tableName, IReadOnlyCollection<TKey> keys, string keyColumn, CancellationToken cancellationToken = default)
    {
        if (keys is null || keys.Count == 0) return 0;
        return await ForgeProviderBulkFallback.DeleteRowsAsync(connection, tableName, keys as IReadOnlyList<TKey> ?? keys.ToArray(), keyColumn, cancellationToken).ConfigureAwait(false);
    }
}
