using System.Data.Common;

namespace ForgeORM.Core;

internal static class BulkFallback
{
    public static ValueTask InsertAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken)
    {
        if (rows is null || rows.Count == 0)
            return default;

        var list = rows as IReadOnlyList<T> ?? rows.ToArray();
        var task = ForgeProviderBulkFallback.InsertRowsAsync(connection, tableName, list, cancellationToken);
        return task.IsCompletedSuccessfully ? default : AwaitDiscard(task);
    }

    public static ValueTask UpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        CancellationToken cancellationToken)
    {
        if (rows is null || rows.Count == 0)
            return default;

        var list = rows as IReadOnlyList<T> ?? rows.ToArray();
        var task = ForgeProviderBulkFallback.UpdateRowsAsync(connection, tableName, list, keyColumn, cancellationToken);
        return task.IsCompletedSuccessfully ? default : AwaitDiscard(task);
    }

    public static ValueTask DeleteAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken)
    {
        if (keys is null || keys.Count == 0)
            return default;

        var list = keys as IReadOnlyList<TKey> ?? keys.ToArray();
        var task = ForgeProviderBulkFallback.DeleteRowsAsync(connection, tableName, list, keyColumn, cancellationToken);
        return task.IsCompletedSuccessfully ? default : AwaitDiscard(task);
    }

    private static async ValueTask AwaitDiscard(ValueTask<int> task)
    {
        _ = await task.ConfigureAwait(false);
    }
}
