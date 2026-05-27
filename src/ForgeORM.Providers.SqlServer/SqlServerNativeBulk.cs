using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace ForgeORM.Providers.SqlServer;

internal static class SqlServerNativeBulk
{
    public static async ValueTask BulkInsertAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0)
            return;

        if (connection is not SqlConnection sqlConnection)
        {
            await BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (sqlConnection.State != ConnectionState.Open)
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var list = rows as IReadOnlyList<T> ?? rows.ToArray();
        var properties = GetBulkProperties<T>(includeIdentity: false);
        if (properties.Length == 0)
            return;

        var table = CreateDataTable(properties, list);

        using var bulk = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.CheckConstraints, externalTransaction: null)
        {
            DestinationTableName = tableName,
            BatchSize = Math.Min(Math.Max(list.Count, 1), 5000),
            BulkCopyTimeout = 0,
            EnableStreaming = true
        };

        for (var i = 0; i < properties.Length; i++)
            bulk.ColumnMappings.Add(properties[i].Name, properties[i].Name);

        await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> BulkUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return 0;

        if (connection is not SqlConnection sqlConnection)
        {
            await BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
            return rows.Count;
        }

        if (sqlConnection.State != ConnectionState.Open)
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var properties = GetBulkProperties<T>(includeIdentity: true);
        var key = FindProperty(properties, keyColumn);
        if (key is null)
            throw new InvalidOperationException($"Bulk update requires key column '{keyColumn}' on '{typeof(T).Name}'.");

        var updateProperties = properties
            .Where(p => !string.Equals(p.Name, key.Name, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (updateProperties.Length == 0)
            return 0;

        var tempTable = "#ForgeBulkUpdate_" + Guid.NewGuid().ToString("N");
        var table = CreateDataTable(properties, rows);

        await using var create = sqlConnection.CreateCommand();
        create.CommandText = BuildTempTableSql(tempTable, properties);
        await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        using (var bulk = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock, externalTransaction: null)
        {
            DestinationTableName = tempTable,
            BatchSize = Math.Min(Math.Max(rows.Count, 1), 5000),
            BulkCopyTimeout = 0,
            EnableStreaming = true
        })
        {
            for (var i = 0; i < properties.Length; i++)
                bulk.ColumnMappings.Add(properties[i].Name, properties[i].Name);

            await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
        }

        await using var merge = sqlConnection.CreateCommand();
        merge.CommandText = BuildMergeUpdateSql(tableName, tempTable, key.Name, updateProperties);
        return await merge.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> BulkDeleteAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return 0;

        if (connection is not SqlConnection sqlConnection)
        {
            await BulkFallback.DeleteAsync(connection, tableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
            return keys.Count;
        }

        if (sqlConnection.State != ConnectionState.Open)
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var tempTable = "#ForgeBulkDelete_" + Guid.NewGuid().ToString("N");
        var keyType = Nullable.GetUnderlyingType(typeof(TKey)) ?? typeof(TKey);
        if (keyType.IsEnum)
            keyType = typeof(string);

        var table = new DataTable();
        table.Columns.Add(keyColumn, keyType);

        for (var i = 0; i < keys.Count; i++)
        {
            var row = table.NewRow();
            row[0] = NormalizeValue(keys[i], typeof(TKey)) ?? DBNull.Value;
            table.Rows.Add(row);
        }

        await using var create = sqlConnection.CreateCommand();
        create.CommandText = $"CREATE TABLE {tempTable} ({QuoteIdentifier(keyColumn)} {ToSqlType(keyType)} NULL);";
        await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        using (var bulk = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock, externalTransaction: null)
        {
            DestinationTableName = tempTable,
            BatchSize = Math.Min(Math.Max(keys.Count, 1), 5000),
            BulkCopyTimeout = 0,
            EnableStreaming = true
        })
        {
            bulk.ColumnMappings.Add(keyColumn, keyColumn);
            await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
        }

        await using var delete = sqlConnection.CreateCommand();
        delete.CommandText =
            $"DELETE Target FROM {QuoteTable(tableName)} AS Target INNER JOIN {tempTable} AS Source ON Target.{QuoteIdentifier(keyColumn)} = Source.{QuoteIdentifier(keyColumn)};";
        return await delete.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static PropertyInfo[] GetBulkProperties<T>(bool includeIdentity)
    {
        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType));

        if (!includeIdentity)
            props = props.Where(p => !IsIdentityConvention(p));

        return props.ToArray();
    }

    private static PropertyInfo? FindProperty(PropertyInfo[] properties, string name)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            if (string.Equals(properties[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return properties[i];
        }

        return null;
    }

    private static DataTable CreateDataTable<T>(PropertyInfo[] properties, IReadOnlyList<T> rows)
    {
        var table = new DataTable();

        for (var i = 0; i < properties.Length; i++)
        {
            var type = Nullable.GetUnderlyingType(properties[i].PropertyType) ?? properties[i].PropertyType;
            if (type.IsEnum)
                type = typeof(string);
            if (type == typeof(DateOnly) || type == typeof(TimeOnly))
                type = typeof(string);

            table.Columns.Add(properties[i].Name, type);
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var dataRow = table.NewRow();

            for (var c = 0; c < properties.Length; c++)
            {
                dataRow[c] = NormalizeValue(ForgeProviderAccessors.Get(properties[c], rows[r]!), properties[c].PropertyType) ?? DBNull.Value;
            }

            table.Rows.Add(dataRow);
        }

        return table;
    }

    private static string BuildTempTableSql(string tempTable, PropertyInfo[] properties)
    {
        var sql = new System.Text.StringBuilder(256 + properties.Length * 64);
        sql.Append("CREATE TABLE ").Append(tempTable).Append(" (");

        for (var i = 0; i < properties.Length; i++)
        {
            if (i > 0)
                sql.Append(", ");

            var type = Nullable.GetUnderlyingType(properties[i].PropertyType) ?? properties[i].PropertyType;
            sql.Append(QuoteIdentifier(properties[i].Name)).Append(' ').Append(ToSqlType(type)).Append(" NULL");
        }

        sql.Append(");");
        return sql.ToString();
    }

    private static string BuildMergeUpdateSql(string tableName, string sourceTable, string keyColumn, PropertyInfo[] updateProperties)
    {
        var sql = new System.Text.StringBuilder(512 + updateProperties.Length * 64);
        var key = QuoteIdentifier(keyColumn);

        sql.Append("MERGE ").Append(QuoteTable(tableName)).Append(" AS Target ")
           .Append("USING ").Append(sourceTable).Append(" AS Source ")
           .Append("ON Target.").Append(key).Append(" = Source.").Append(key).Append(' ')
           .Append("WHEN MATCHED THEN UPDATE SET ");

        for (var i = 0; i < updateProperties.Length; i++)
        {
            if (i > 0)
                sql.Append(", ");

            var col = QuoteIdentifier(updateProperties[i].Name);
            sql.Append("Target.").Append(col).Append(" = Source.").Append(col);
        }

        sql.Append(';');
        return sql.ToString();
    }

    private static string ToSqlType(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;

        if (actual.IsEnum) return "NVARCHAR(128)";
        if (actual == typeof(string)) return "NVARCHAR(MAX)";
        if (actual == typeof(int)) return "INT";
        if (actual == typeof(long)) return "BIGINT";
        if (actual == typeof(short)) return "SMALLINT";
        if (actual == typeof(byte)) return "TINYINT";
        if (actual == typeof(bool)) return "BIT";
        if (actual == typeof(decimal)) return "DECIMAL(38, 10)";
        if (actual == typeof(float)) return "REAL";
        if (actual == typeof(double)) return "FLOAT";
        if (actual == typeof(DateTime)) return "DATETIME2";
        if (actual == typeof(DateTimeOffset)) return "DATETIMEOFFSET";
        if (actual == typeof(Guid)) return "UNIQUEIDENTIFIER";
        if (actual == typeof(byte[])) return "VARBINARY(MAX)";
        if (actual == typeof(TimeSpan)) return "TIME";
        if (actual == typeof(DateOnly)) return "NVARCHAR(32)";
        if (actual == typeof(TimeOnly)) return "NVARCHAR(32)";

        return "NVARCHAR(MAX)";
    }

    private static object? NormalizeValue(object? value, Type declaredType)
    {
        if (value is null)
            return null;

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual.IsEnum)
            return value.ToString();

        if (actual == typeof(DateOnly) && value is DateOnly dateOnly)
            return dateOnly.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        if (actual == typeof(TimeOnly) && value is TimeOnly timeOnly)
            return timeOnly.ToString("HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);

        if (actual == typeof(DateTime) && value is DateTime dt)
            return dt == default || dt < new DateTime(1753, 1, 1) ? DateTime.UtcNow : dt;

        if (actual == typeof(DateTimeOffset) && value is DateTimeOffset dto)
            return dto == default ? DateTimeOffset.UtcNow : dto;

        return value;
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

    private static bool IsIdentityConvention(PropertyInfo property)
    {
        var entityName = property.DeclaringType?.Name + "Id";
        return property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
            || property.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase)
            || property.GetCustomAttributes(false).Any(a => a.GetType().Name is "ForgeKeyAttribute" or "KeyAttribute" or "ForgeIdentityAttribute" or "DatabaseGeneratedAttribute");
    }

    private static string QuoteTable(string tableName)
    {
        var parts = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
            return QuoteIdentifier(parts[0]);

        return string.Join(".", parts.Select(QuoteIdentifier));
    }

    private static string QuoteIdentifier(string name)
    {
        var clean = name.Trim('[', ']');
        return "[" + clean.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }
}
