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