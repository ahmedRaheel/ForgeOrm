using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

internal static class SqlServerNativeBulk
{
    public static async ValueTask BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return;

        if (connection is not SqlConnection sqlConnection)
        {
            await BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (sqlConnection.State != ConnectionState.Open)
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var props = GetBulkProperties<T>();
        var table = new DataTable();
        foreach (var prop in props)
            table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

        foreach (var row in rows)
        {
            var values = new object?[props.Length];
            for (var i = 0; i < props.Length; i++)
                values[i] = NormalizeValue(ForgeProviderAccessors.Get(props[i], row!), props[i].PropertyType) ?? DBNull.Value;
            table.Rows.Add(values);
        }

        using var bulk = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.CheckConstraints, externalTransaction: null)
        {
            DestinationTableName = tableName,
            BatchSize = Math.Min(Math.Max(rows.Count, 1), 5000),
            BulkCopyTimeout = 0,
            EnableStreaming = true
        };

        foreach (var prop in props)
            bulk.ColumnMappings.Add(prop.Name, prop.Name);

        await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
    }

    private static PropertyInfo[] GetBulkProperties<T>()
        => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType))
            .Where(p => !IsIdentityConvention(p))
            .ToArray();

    private static bool IsIdentityConvention(PropertyInfo property)
    {
        var name = property.Name;
        var entityName = property.DeclaringType?.Name + "Id";
        return name.Equals("Id", StringComparison.OrdinalIgnoreCase)
               || name.Equals(entityName, StringComparison.OrdinalIgnoreCase)
               || property.GetCustomAttributes().Any(a => a.GetType().Name is "ForgeKeyAttribute" or "KeyAttribute");
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

    private static object? NormalizeValue(object? value, Type declaredType)
    {
        if (value is null)
            return null;

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        if (actual.IsEnum)
            return value.ToString();

        if (actual == typeof(DateTime))
        {
            var dateTime = (DateTime)value;
            return dateTime == default || dateTime < new DateTime(1753, 1, 1) ? DateTime.UtcNow : dateTime;
        }

        if (actual == typeof(DateTimeOffset))
        {
            var dto = (DateTimeOffset)value;
            return dto == default ? DateTimeOffset.UtcNow : dto;
        }

        return value;
    }
}
