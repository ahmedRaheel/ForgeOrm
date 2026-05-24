using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeORM.Core;

internal static class BulkFallback
{
    private static readonly ConcurrentDictionary<(Type Type, string Table, string Key), string> UpdateSqlStatementCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string Table), string> InsertSqlStatementCache = new();

    public static ValueTask InsertAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken)
    {
        if (rows is null || rows.Count == 0)
            return default;

        var sql = InsertSqlStatementCache.GetOrAdd((typeof(T), tableName), static key =>
        {
            var props = ForgeProviderAdo.PropertyCache<T>.Properties;
            if (props.Length == 0)
                throw new InvalidOperationException($"Type {key.Type.Name} has no valid scalar properties to insert.");

            var columns = string.Join(", ", props.Select(p => p.Info.Name));
            var values = string.Join(", ", props.Select(p => p.ParamName));
            return $"INSERT INTO {key.Table} ({columns}) VALUES ({values})";
        });

        var task = ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, cancellationToken);
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

        var sql = UpdateSqlStatementCache.GetOrAdd((typeof(T), tableName, keyColumn), static key =>
        {
            var (type, table, pk) = key;
            var props = ForgeProviderAdo.PropertyCache<T>.Properties
                .Where(p => !p.Info.Name.Equals(pk, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Info.Name)
                .ToArray();

            if (props.Length == 0)
                throw new InvalidOperationException($"Type {type.Name} has no valid properties to update.");

            var set = string.Join(", ", props.Select(p => $"{p} = @{p}"));
            return $"UPDATE {table} SET {set} WHERE {pk} = @{pk}";
        });

        var task = ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, cancellationToken);
        return task.IsCompletedSuccessfully ? default : AwaitDiscard(task);
    }

    private static async ValueTask AwaitDiscard(ValueTask<int> task)
    {
        _ = await task.ConfigureAwait(false);
    }
}
