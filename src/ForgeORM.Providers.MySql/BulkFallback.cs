using ForgeORM.Abstractions;
using ForgeORM.Core;
using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static ForgeORM.Providers.MySql.ForgeProviderAdo;

namespace ForgeORM.Providers.MySql;

internal static class BulkFallback
{
    private static readonly ConcurrentDictionary<(Type Type, string Table, string Key), string> UpdateSqlStatementCache = new();

    public static ValueTask UpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken ct)
    {
        var sql = UpdateSqlStatementCache.GetOrAdd((typeof(T), tableName, keyColumn), static key =>
        {
            var (type, table, pk) = key;
            var props = PropertyCache<T>.Properties
                .Where(p => !p.Info.Name.Equals(pk, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Info.Name)
                .ToList();

            if (props.Count == 0)
                throw new InvalidOperationException($"Type {type.Name} has no valid properties to update.");

            var set = string.Join(", ", props.Select(p => $"{p} = @{p}"));
            return $"UPDATE {table} SET {set} WHERE {pk} = @{pk}";
        });

        // Discard the task's integer result to return a clean, non-allocating ValueTask wrapper
        var task = ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
        return task.IsCompletedSuccessfully ? ValueTask.CompletedTask : ConvertToValueTask(task);
    }

    private static async ValueTask ConvertToValueTask(ValueTask<int> task) => await task.ConfigureAwait(false);
}
