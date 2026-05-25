using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
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

            var capacity = key.Table.Length + (props.Length * 32) + 32;
            var columns = new StringBuilder(capacity);
            var values = new StringBuilder(capacity);

            for (var i = 0; i < props.Length; i++)
            {
                if (i > 0)
                {
                    columns.Append(", ");
                    values.Append(", ");
                }

                columns.Append(props[i].Info.Name);
                values.Append(props[i].ParamName);
            }

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
            var props = ForgeProviderAdo.PropertyCache<T>.Properties;
            var set = new StringBuilder(table.Length + (props.Length * 32) + 32);
            var count = 0;

            for (var i = 0; i < props.Length; i++)
            {
                var name = props[i].Info.Name;
                if (name.Equals(pk, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (count > 0)
                    set.Append(", ");

                set.Append(name).Append(" = @").Append(name);
                count++;
            }

            if (count == 0)
                throw new InvalidOperationException($"Type {type.Name} has no valid properties to update.");

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
