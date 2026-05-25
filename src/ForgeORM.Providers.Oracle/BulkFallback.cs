using ForgeORM.Abstractions;
using ForgeORM.Core;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ForgeORM.Providers.Oracle;

internal static class BulkFallback
{
    private static readonly ConcurrentDictionary<(Type Type, string Table, string Key), string> UpdateSqlCache = new();
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async ValueTask InsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken ct)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && IsScalar(p.PropertyType)).ToList();
        var columns = string.Join(", ", props.Select(p => p.Name));
        var values = string.Join(", ", props.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
        return ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ValueTask UpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        CancellationToken ct)
    {
        // 1. Guard clause to avoid processing overhead or state machine instantiation
        if (rows == null || rows.Count == 0)
            return ValueTask.CompletedTask;

        // 2. Retrieve or compile the SQL update string. This executes exactly ONCE per unique schema setup.
        var sql = UpdateSqlCache.GetOrAdd((typeof(T), tableName, keyColumn), static key =>
        {
            var (type, table, pk) = key;

            // Extract all updateable properties (excluding the primary key column)
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsScalar(p.PropertyType) && !p.Name.Equals(pk, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name)
                .ToList();

            if (props.Count == 0)
                throw new InvalidOperationException($"Type {type.Name} has no valid scalar properties to update.");

            // Build the SET clause: Field1 = @Field1, Field2 = @Field2
            var setClause = string.Join(", ", props.Select(p => $"{p} = @{p}"));

            // Build the final optimized SQL statement
            return $"UPDATE {table} SET {setClause} WHERE {pk} = @{pk}";
        });

        // 3. Perfect-forward the ValueTask straight down to the optimized batch executor.
        // This elides the async state machine wrapper allocation entirely.
        return ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
    }
    private static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive
               || actual.IsEnum
               || actual == typeof(string)
               || actual == typeof(Guid)
               || actual == typeof(decimal)
               || actual == typeof(DateTime)
               || actual == typeof(DateTimeOffset)
               || actual == typeof(DateOnly)
               || actual == typeof(TimeOnly)
               || actual == typeof(TimeSpan)
               || actual == typeof(byte[]);
    }

}
