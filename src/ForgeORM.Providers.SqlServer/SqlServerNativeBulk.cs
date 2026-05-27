using ForgeORM.Core;
using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace ForgeORM.Providers.SqlServer;

internal static class SqlServerNativeBulk
{
    public static async ValueTask BulkInsertAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        CancellationToken cancellationToken = default)
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

        var plan = SqlServerBulkPlanCache<T>.GetOrCreate(tableName, keyColumn: "Id", includeKey: false);

        await EnsureTableTypeAsync(sqlConnection, plan, cancellationToken).ConfigureAwait(false);

        await using var command = sqlConnection.CreateCommand();
        command.CommandText = plan.InsertSql;
        command.CommandType = CommandType.Text;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = new SqlServerDataRecordEnumerable<T>(rows, plan);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask BulkUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return;

        if (connection is not SqlConnection sqlConnection)
        {
            await BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (sqlConnection.State != ConnectionState.Open)
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var plan = SqlServerBulkPlanCache<T>.GetOrCreate(tableName, keyColumn, includeKey: true);

        await EnsureTableTypeAsync(sqlConnection, plan, cancellationToken).ConfigureAwait(false);

        await using var command = sqlConnection.CreateCommand();
        command.CommandText = plan.MergeUpdateSql;
        command.CommandType = CommandType.Text;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = new SqlServerDataRecordEnumerable<T>(rows, plan);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask BulkMergeAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        await BulkUpdateAsync(connection, tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask EnsureTableTypeAsync<T>(
        SqlConnection connection,
        SqlServerBulkPlan<T> plan,
        CancellationToken cancellationToken)
    {
        if (!SqlServerBulkTypeCache.TryMarkEnsured(plan.TvpTypeName))
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = plan.EnsureTypeSql;
        command.CommandType = CommandType.Text;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal static class SqlServerBulkTypeCache
{
    private static readonly ConcurrentDictionary<string, byte> Ensured = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryMarkEnsured(string typeName)
        => Ensured.TryAdd(typeName, 0);
}

internal static class SqlServerBulkPlanCache<T>
{
    private static readonly ConcurrentDictionary<string, SqlServerBulkPlan<T>> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static SqlServerBulkPlan<T> GetOrCreate(string tableName, string keyColumn, bool includeKey)
    {
        var key = tableName + "|" + keyColumn + "|" + includeKey;
        return Cache.GetOrAdd(key, _ => SqlServerBulkPlan<T>.Create(tableName, keyColumn, includeKey));
    }
}

internal sealed class SqlServerBulkPlan<T>
{
    private SqlServerBulkPlan(
        string tableName,
        string keyColumn,
        string tvpTypeName,
        PropertyInfo[] properties,
        SqlMetaData[] metadata,
        string ensureTypeSql,
        string insertSql,
        string mergeUpdateSql)
    {
        TableName = tableName;
        KeyColumn = keyColumn;
        TvpTypeName = tvpTypeName;
        Properties = properties;
        MetaData = metadata;
        EnsureTypeSql = ensureTypeSql;
        InsertSql = insertSql;
        MergeUpdateSql = mergeUpdateSql;
    }

    public string TableName { get; }

    public string KeyColumn { get; }

    public string TvpTypeName { get; }

    public PropertyInfo[] Properties { get; }

    public SqlMetaData[] MetaData { get; }

    public string EnsureTypeSql { get; }

    public string InsertSql { get; }

    public string MergeUpdateSql { get; }

    public static SqlServerBulkPlan<T> Create(string tableName, string keyColumn, bool includeKey)
    {
        var properties = GetBulkProperties(includeKey, keyColumn);
        var metadata = new SqlMetaData[properties.Length];

        for (var i = 0; i < properties.Length; i++)
            metadata[i] = CreateMetaData(properties[i]);

        var shortName = UnqualifiedName(tableName).Trim('[', ']');
        var tvpTypeName = $"dbo.{shortName}TableType";

        var escapedTable = EscapeTableName(tableName);
        var escapedColumns = properties.Select(p => EscapeIdentifier(p.Name)).ToArray();
        var columnList = string.Join(", ", escapedColumns);

        var insertSql = $"INSERT INTO {escapedTable} ({columnList}) SELECT {columnList} FROM @Rows;";

        var updateColumns = properties
            .Where(p => !p.Name.Equals(keyColumn, StringComparison.OrdinalIgnoreCase))
            .Select(p => $"Target.{EscapeIdentifier(p.Name)} = Source.{EscapeIdentifier(p.Name)}")
            .ToArray();

        var mergeSql = $"""
MERGE {escapedTable} AS Target
USING @Rows AS Source
ON Target.{EscapeIdentifier(keyColumn)} = Source.{EscapeIdentifier(keyColumn)}
WHEN MATCHED THEN
    UPDATE SET {string.Join(", ", updateColumns)};
""";

        var definitions = new string[properties.Length];
        for (var i = 0; i < properties.Length; i++)
            definitions[i] = $"{EscapeIdentifier(properties[i].Name)} {GetSqlTypeDefinition(properties[i].PropertyType)} NULL";

        var ensureSql = $"""
IF TYPE_ID(N'{tvpTypeName}') IS NULL
    EXEC(N'CREATE TYPE {tvpTypeName} AS TABLE ({string.Join(", ", definitions).Replace("'", "''")})');
""";

        return new SqlServerBulkPlan<T>(
            tableName,
            keyColumn,
            tvpTypeName,
            properties,
            metadata,
            ensureSql,
            insertSql,
            mergeSql);
    }

    private static PropertyInfo[] GetBulkProperties(bool includeKey, string keyColumn)
    {
        return typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType))
            .Where(p => includeKey || !IsKey(p, keyColumn))
            .ToArray();
    }

    private static bool IsKey(PropertyInfo property, string keyColumn)
    {
        var entityName = property.DeclaringType?.Name + "Id";

        return property.Name.Equals(keyColumn, StringComparison.OrdinalIgnoreCase)
               || property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
               || property.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase)
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

    private static SqlMetaData CreateMetaData(PropertyInfo property)
    {
        var actual = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (actual.IsEnum)
            return new SqlMetaData(property.Name, SqlDbType.NVarChar, 128);

        if (actual == typeof(int))
            return new SqlMetaData(property.Name, SqlDbType.Int);

        if (actual == typeof(long))
            return new SqlMetaData(property.Name, SqlDbType.BigInt);

        if (actual == typeof(short))
            return new SqlMetaData(property.Name, SqlDbType.SmallInt);

        if (actual == typeof(byte))
            return new SqlMetaData(property.Name, SqlDbType.TinyInt);

        if (actual == typeof(bool))
            return new SqlMetaData(property.Name, SqlDbType.Bit);

        if (actual == typeof(decimal))
            return new SqlMetaData(property.Name, SqlDbType.Decimal, (byte)18, (byte)4);

        if (actual == typeof(double))
            return new SqlMetaData(property.Name, SqlDbType.Float);

        if (actual == typeof(float))
            return new SqlMetaData(property.Name, SqlDbType.Real);

        if (actual == typeof(DateTime))
            return new SqlMetaData(property.Name, SqlDbType.DateTime2);

        if (actual == typeof(DateTimeOffset))
            return new SqlMetaData(property.Name, SqlDbType.DateTimeOffset);

        if (actual == typeof(Guid))
            return new SqlMetaData(property.Name, SqlDbType.UniqueIdentifier);

        if (actual == typeof(byte[]))
            return new SqlMetaData(property.Name, SqlDbType.VarBinary, -1);

        return new SqlMetaData(property.Name, SqlDbType.NVarChar, -1);
    }

    private static string GetSqlTypeDefinition(Type declaredType)
    {
        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual.IsEnum)
            return "NVARCHAR(128)";

        if (actual == typeof(int))
            return "INT";

        if (actual == typeof(long))
            return "BIGINT";

        if (actual == typeof(short))
            return "SMALLINT";

        if (actual == typeof(byte))
            return "TINYINT";

        if (actual == typeof(bool))
            return "BIT";

        if (actual == typeof(decimal))
            return "DECIMAL(18,4)";

        if (actual == typeof(double))
            return "FLOAT";

        if (actual == typeof(float))
            return "REAL";

        if (actual == typeof(DateTime))
            return "DATETIME2";

        if (actual == typeof(DateTimeOffset))
            return "DATETIMEOFFSET";

        if (actual == typeof(Guid))
            return "UNIQUEIDENTIFIER";

        if (actual == typeof(byte[]))
            return "VARBINARY(MAX)";

        return "NVARCHAR(MAX)";
    }

    private static string EscapeTableName(string tableName)
        => string.Join('.', tableName.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(EscapeIdentifier));

    private static string EscapeIdentifier(string name)
        => "[" + name.Trim('[', ']').Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string UnqualifiedName(string tableName)
    {
        var parts = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? tableName : parts[^1];
    }
}

internal sealed class SqlServerDataRecordEnumerable<T> : IEnumerable<SqlDataRecord>
{
    private readonly IReadOnlyCollection<T> _items;
    private readonly SqlServerBulkPlan<T> _plan;

    public SqlServerDataRecordEnumerable(IReadOnlyCollection<T> items, SqlServerBulkPlan<T> plan)
    {
        _items = items;
        _plan = plan;
    }

    public IEnumerator<SqlDataRecord> GetEnumerator()
    {
        foreach (var item in _items)
        {
            var record = new SqlDataRecord(_plan.MetaData);
            SetValues(record, _plan.Properties, item!);
            yield return record;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void SetValues(SqlDataRecord record, PropertyInfo[] properties, object item)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var value = ForgeProviderAccessors.Get(property, item);
            SetTypedValue(record, i, value, property.PropertyType);
        }
    }

    private static void SetTypedValue(SqlDataRecord record, int ordinal, object? value, Type declaredType)
    {
        if (value is null)
        {
            record.SetDBNull(ordinal);
            return;
        }

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual.IsEnum)
        {
            record.SetString(ordinal, value.ToString() ?? string.Empty);
            return;
        }

        if (actual == typeof(int))
        {
            record.SetInt32(ordinal, Convert.ToInt32(value));
            return;
        }

        if (actual == typeof(long))
        {
            record.SetInt64(ordinal, Convert.ToInt64(value));
            return;
        }

        if (actual == typeof(short))
        {
            record.SetInt16(ordinal, Convert.ToInt16(value));
            return;
        }

        if (actual == typeof(byte))
        {
            record.SetByte(ordinal, Convert.ToByte(value));
            return;
        }

        if (actual == typeof(bool))
        {
            record.SetBoolean(ordinal, Convert.ToBoolean(value));
            return;
        }

        if (actual == typeof(decimal))
        {
            record.SetDecimal(ordinal, Convert.ToDecimal(value));
            return;
        }

        if (actual == typeof(double))
        {
            record.SetDouble(ordinal, Convert.ToDouble(value));
            return;
        }

        if (actual == typeof(float))
        {
            record.SetFloat(ordinal, Convert.ToSingle(value));
            return;
        }

        if (actual == typeof(DateTime))
        {
            var dateTime = (DateTime)value;
            record.SetDateTime(ordinal, dateTime == default || dateTime < new DateTime(1753, 1, 1) ? DateTime.UtcNow : dateTime);
            return;
        }

        if (actual == typeof(DateTimeOffset))
        {
            record.SetDateTimeOffset(ordinal, (DateTimeOffset)value);
            return;
        }

        if (actual == typeof(Guid))
        {
            record.SetGuid(ordinal, (Guid)value);
            return;
        }

        if (actual == typeof(byte[]))
        {
            record.SetBytes(ordinal, 0, (byte[])value, 0, ((byte[])value).Length);
            return;
        }

        record.SetString(ordinal, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
    }
}
