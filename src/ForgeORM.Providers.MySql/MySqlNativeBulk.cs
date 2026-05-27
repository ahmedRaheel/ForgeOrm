using ForgeORM.Core;
using System.Data.Common;
using System.Reflection;

namespace ForgeORM.Providers.MySql;

internal static class MySqlNativeBulk
{
    public static async ValueTask BulkInsertAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0)
            return;

        await BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken)
            .ConfigureAwait(false);
    }


    public static ValueTask BulkUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0)
            return ValueTask.CompletedTask;

        // Provider-native strategy placeholder:
        // PostgreSQL: COPY to temp table + UPDATE FROM / ON CONFLICT.
        // MySQL: temp table + UPDATE JOIN / ON DUPLICATE KEY UPDATE.
        // Oracle: array binding + MERGE.
        return BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);
    }

    public static ValueTask BulkMergeAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
        => BulkUpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);

    internal static PropertyInfo[] GetBulkProperties<T>()
        => typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType))
            .ToArray();

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