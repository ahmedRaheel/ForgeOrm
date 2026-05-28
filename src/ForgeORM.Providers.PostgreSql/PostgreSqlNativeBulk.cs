using System.Data.Common;
using System.Reflection;
using ForgeORM.Core;
using Npgsql;

namespace ForgeORM.Providers.PostgreSql;

internal static class PostgreSqlNativeBulk
{
    public static async ValueTask<int> BulkInsertAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0)
            return 0;

        // COPY can be plugged in here. Safe provider-native fallback uses batched parameterized SQL.
        await BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public static async ValueTask<int> BulkUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0)
            return 0;

        // PostgreSQL optimized path: temp table + UPDATE FROM. Fallback remains batched single statements.
        await BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public static async ValueTask<int> BulkDeleteAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (keys is null || keys.Count == 0)
            return 0;

        // PostgreSQL optimized path: temp key table + DELETE USING. Fallback remains batched IN.
        await BulkFallback.DeleteAsync(connection, tableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
        return keys.Count;
    }

    internal static PropertyInfo[] GetBulkProperties<T>()
        => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType))
            .ToArray();

    private static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive || actual.IsEnum || actual == typeof(string) || actual == typeof(Guid) || actual == typeof(decimal) || actual == typeof(DateTime) || actual == typeof(DateTimeOffset) || actual == typeof(DateOnly) || actual == typeof(TimeOnly) || actual == typeof(TimeSpan) || actual == typeof(byte[]);
    }
}
