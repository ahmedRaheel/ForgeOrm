using System.Data.Common;
using System.Reflection;
using Npgsql;

namespace ForgeORM.Providers.PostgreSql;

internal static class PostgreSqlNativeBulk
{
    public static async ValueTask BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return;

        // Provider-native hook: Npgsql COPY support can be enabled here per deployment.
        // The safe default uses the existing batched parameterized path to preserve correctness.
        // This keeps public API stable while separating PostgreSQL bulk from generic ORM execution.
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
