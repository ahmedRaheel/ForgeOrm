using System.Data.Common;
using System.Reflection;
using MySqlConnector;

namespace ForgeORM.Providers.MySql;

internal static class MySqlNativeBulk
{
    public static async Task BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return;

        // Provider-native hook: MySQL optimized multi-row INSERT/LOAD DATA can be enabled here.
        // Safe default delegates to existing batched path so the provider remains buildable and correct.
        await BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken).ConfigureAwait(false);
    }

    internal static PropertyInfo[] GetBulkProperties<T>()
        => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType))
            .ToArray();

    private static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive || actual.IsEnum || actual == typeof(string) || actual == typeof(Guid) || actual == typeof(decimal) || actual == typeof(DateTime) || actual == typeof(DateTimeOffset) || actual == typeof(byte[]);
    }
}
