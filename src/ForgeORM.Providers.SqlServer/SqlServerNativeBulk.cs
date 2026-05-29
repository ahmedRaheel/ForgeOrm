using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace ForgeORM.Providers.SqlServer;

internal static class SqlServerNativeBulk
{
    private static readonly ConcurrentDictionary<(Type EntityType, string TableName), SqlServerInsertPlan> InsertPlanCache = new();
    private static readonly ConcurrentDictionary<(Type EntityType, string TableName, string KeyColumn), SqlServerUpdatePlan> UpdatePlanCache = new();
    private static readonly ConcurrentDictionary<(Type KeyType, string TableName, string KeyColumn), SqlServerDeletePlan> DeletePlanCache = new();

    public static async ValueTask BulkInsertAsync<T>(
     DbConnection connection,
     string tableName,
     IReadOnlyCollection<T> rows,
     ForgeProviderBulkOptions options,
     CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0)
            return;

        var list = rows as IReadOnlyList<T> ?? rows.ToArray();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var plan = InsertPlanCache.GetOrAdd(
            (typeof(T), tableName),
            static key => SqlServerInsertPlan.Create(key.EntityType, key.TableName));

        if (plan.Properties.Length == 0)
            return;

        switch (options.SqlServerStrategy)
        {
            case ForgeBulkOperationStrategy.TableTypeParameter:

                await InsertWithDataTableFallbackAsync(
                    (SqlConnection)connection,
                    plan,
                    list, 
                    options,
                    cancellationToken).ConfigureAwait(false);

                break;

            case ForgeBulkOperationStrategy.SqlBulkCopy:

                await BulkFallback.InsertAsync(
                    connection,
                    tableName,
                    rows,
                    cancellationToken).ConfigureAwait(false);

                break;

            default:

                await InsertWithSqlDataRecordTvpAsync(
                    (SqlConnection)connection,
                    plan,
                    list,
                    cancellationToken).ConfigureAwait(false);

                break;
        }
    }

    public static async ValueTask<int> BulkUpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn,
        ForgeProviderBulkOptions options,
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

        var plan = UpdatePlanCache.GetOrAdd(
            (typeof(T), tableName, keyColumn),
            static key => SqlServerUpdatePlan.Create(key.EntityType, key.TableName, key.KeyColumn));

        if (plan.UpdateColumnNames.Length == 0)
            return 0;

        var tempTable = "#ForgeBulkUpdate_" + Guid.NewGuid().ToString("N");
        var table = plan.CreateTable(rows);

        await using var create = sqlConnection.CreateCommand();
        create.CommandText = plan.CreateTempTableSql(tempTable);
        await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        using (var bulk = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock, externalTransaction: null)
        {
            DestinationTableName = tempTable,
            BatchSize = options.BatchSize > 0 ? Math.Min(Math.Max(rows.Count, 1), options.BatchSize) : Math.Min(Math.Max(rows.Count, 1), 5000),
            BulkCopyTimeout = options.CommandTimeoutSeconds,
            EnableStreaming = true
        })
        {
            for (var i = 0; i < plan.ColumnNames.Length; i++)
                bulk.ColumnMappings.Add(plan.ColumnNames[i], plan.ColumnNames[i]);

            await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
        }

        await using var merge = sqlConnection.CreateCommand();
        merge.CommandText = plan.CreateMergeSql(tempTable);
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

        var plan = DeletePlanCache.GetOrAdd(
            (typeof(TKey), tableName, keyColumn),
            static key => SqlServerDeletePlan.Create(key.KeyType, key.TableName, key.KeyColumn));

        var tempTable = "#ForgeBulkDelete_" + Guid.NewGuid().ToString("N");
        var table = plan.CreateTable(keys);

        await using var create = sqlConnection.CreateCommand();
        create.CommandText = plan.CreateTempTableSql(tempTable);
        await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        using (var bulk = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock, externalTransaction: null)
        {
            DestinationTableName = tempTable,
            BatchSize = Math.Min(Math.Max(keys.Count, 1), 5000),
            BulkCopyTimeout = 0,
            EnableStreaming = true
        })
        {
            bulk.ColumnMappings.Add(plan.KeyColumn, plan.KeyColumn);
            await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
        }

        await using var delete = sqlConnection.CreateCommand();
        delete.CommandText = plan.CreateDeleteSql(tempTable);
        return await delete.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
    private static async ValueTask InsertWithSqlDataRecordTvpAsync<T>(
       SqlConnection connection,
       SqlServerInsertPlan plan,
       IReadOnlyList<T> rows,
       CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = plan.InsertSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = new SqlDataRecordRows<T>(rows, plan);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask InsertWithDataTableFallbackAsync<T>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken)
    {
        var table = plan.CreateTable(rows);

        using var bulk = new SqlBulkCopy(
            connection,
            SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.CheckConstraints,
            externalTransaction: null)
        {
            DestinationTableName = plan.QuotedTableName,
            BatchSize = options.BatchSize > 0 ? Math.Min(Math.Max(rows.Count, 1), options.BatchSize) : Math.Min(Math.Max(rows.Count, 1), 5000),
            BulkCopyTimeout = options.CommandTimeoutSeconds,
            EnableStreaming = true
        };

        for (var i = 0; i < plan.ColumnNames.Length; i++)
            bulk.ColumnMappings.Add(plan.ColumnNames[i], plan.ColumnNames[i]);

        await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
    }

    private sealed class SqlServerInsertPlan
    {
        private readonly Func<object, object?>[] _getters;
        private readonly Type[] _declaredTypes;
        private readonly DataTable _schema;

        private SqlServerInsertPlan(
            string quotedTableName,
            string tvpTypeName,
            string insertSql,
            PropertyInfo[] properties,
            string[] columnNames,
            Func<object, object?>[] getters,
            Type[] declaredTypes,
            DataTable schema,
            SqlMetaData[] sqlMetaData)
        {
            QuotedTableName = quotedTableName;
            TvpTypeName = tvpTypeName;
            InsertSql = insertSql;
            Properties = properties;
            ColumnNames = columnNames;
            _getters = getters;
            _declaredTypes = declaredTypes;
            _schema = schema;
            SqlMetaData = sqlMetaData;
        }

        public string QuotedTableName { get; }
        public string TvpTypeName { get; }
        public string InsertSql { get; }
        public SqlMetaData[] SqlMetaData { get; }
        public PropertyInfo[] Properties { get; }
        public string[] ColumnNames { get; }

        public static SqlServerInsertPlan Create(Type entityType, string tableName)
        {
            var properties = GetBulkProperties(entityType, includeIdentity: false);
            var columnNames = BuildColumnNames(properties);
            return new SqlServerInsertPlan(
                QuoteTable(tableName),
                BuildTvpTypeName(entityType),
                BuildInsertFromTvpSql(tableName, columnNames),
                properties,
                columnNames,
                BuildGetters(properties),
                BuildDeclaredTypes(properties),
                CreateSchema(properties),
                BuildSqlMetaData(properties));
        }

        public object? GetValue(object entity, int ordinal) => _getters[ordinal](entity);
        public Type GetDeclaredType(int ordinal) => _declaredTypes[ordinal];

        public DataTable CreateTable<T>(IReadOnlyList<T> rows)
        {
            var table = _schema.Clone();
            table.BeginLoadData();

            for (var r = 0; r < rows.Count; r++)
            {
                var dataRow = table.NewRow();
                var entity = rows[r]!;
                for (var c = 0; c < _getters.Length; c++)
                    dataRow[c] = NormalizeValue(_getters[c](entity), _declaredTypes[c]) ?? DBNull.Value;
                table.Rows.Add(dataRow);
            }

            table.EndLoadData();
            return table;
        }
    }
   
    private sealed class SqlDataRecordRows<T> : IEnumerable<SqlDataRecord>
    {
        private readonly IReadOnlyList<T> _rows;
        private readonly SqlServerInsertPlan _plan;

        public SqlDataRecordRows(IReadOnlyList<T> rows, SqlServerInsertPlan plan)
        {
            _rows = rows;
            _plan = plan;
        }

        public IEnumerator<SqlDataRecord> GetEnumerator()
        {
            for (var r = 0; r < _rows.Count; r++)
            {
                var record = new SqlDataRecord(_plan.SqlMetaData);
                var entity = _rows[r]!;

                for (var c = 0; c < _plan.ColumnNames.Length; c++)
                {
                    var value = NormalizeValue(_plan.GetValue(entity, c), _plan.GetDeclaredType(c));
                    SetRecordValue(record, c, value, _plan.GetDeclaredType(c));
                }

                yield return record;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class SqlServerUpdatePlan
    {
        private readonly Func<object, object?>[] _getters;
        private readonly Type[] _declaredTypes;
        private readonly DataTable _schema;
        private readonly string _tempTableTemplate;
        private readonly string _mergeTemplate;

        private SqlServerUpdatePlan(
            string quotedTableName,
            string keyColumn,
            PropertyInfo[] properties,
            string[] columnNames,
            string[] updateColumnNames,
            Func<object, object?>[] getters,
            Type[] declaredTypes,
            DataTable schema,
            string tempTableTemplate,
            string mergeTemplate)
        {
            QuotedTableName = quotedTableName;
            KeyColumn = keyColumn;
            Properties = properties;
            ColumnNames = columnNames;
            UpdateColumnNames = updateColumnNames;
            _getters = getters;
            _declaredTypes = declaredTypes;
            _schema = schema;
            _tempTableTemplate = tempTableTemplate;
            _mergeTemplate = mergeTemplate;
        }

        public string QuotedTableName { get; }
        public string KeyColumn { get; }
        public PropertyInfo[] Properties { get; }
        public string[] ColumnNames { get; }
        public string[] UpdateColumnNames { get; }

        public static SqlServerUpdatePlan Create(Type entityType, string tableName, string keyColumn)
        {
            var properties = GetBulkProperties(entityType, includeIdentity: true);
            var key = FindProperty(properties, keyColumn)
                ?? throw new InvalidOperationException($"Bulk update requires key column '{keyColumn}' on '{entityType.Name}'.");

            var updateProperties = new List<PropertyInfo>(properties.Length);
            for (var i = 0; i < properties.Length; i++)
            {
                if (!string.Equals(properties[i].Name, key.Name, StringComparison.OrdinalIgnoreCase))
                    updateProperties.Add(properties[i]);
            }

            var columnNames = BuildColumnNames(properties);
            var updateColumnNames = BuildColumnNames(updateProperties.ToArray());
            var tempTemplate = BuildTempTableSqlTemplate(properties);
            var mergeTemplate = BuildMergeUpdateSqlTemplate(tableName, key.Name, updateProperties.ToArray());

            return new SqlServerUpdatePlan(
                QuoteTable(tableName),
                key.Name,
                properties,
                columnNames,
                updateColumnNames,
                BuildGetters(properties),
                BuildDeclaredTypes(properties),
                CreateSchema(properties),
                tempTemplate,
                mergeTemplate);
        }

        public string CreateTempTableSql(string tempTable) => string.Format(System.Globalization.CultureInfo.InvariantCulture, _tempTableTemplate, tempTable);
        public string CreateMergeSql(string tempTable) => string.Format(System.Globalization.CultureInfo.InvariantCulture, _mergeTemplate, tempTable);

        public object? GetValue(object entity, int ordinal) => _getters[ordinal](entity);
        public Type GetDeclaredType(int ordinal) => _declaredTypes[ordinal];

        public DataTable CreateTable<T>(IReadOnlyList<T> rows)
        {
            var table = _schema.Clone();
            table.BeginLoadData();

            for (var r = 0; r < rows.Count; r++)
            {
                var dataRow = table.NewRow();
                var entity = rows[r]!;
                for (var c = 0; c < _getters.Length; c++)
                    dataRow[c] = NormalizeValue(_getters[c](entity), _declaredTypes[c]) ?? DBNull.Value;
                table.Rows.Add(dataRow);
            }

            table.EndLoadData();
            return table;
        }
    }

    private sealed class SqlServerDeletePlan
    {
        private readonly DataTable _schema;
        private readonly Type _keyDeclaredType;
        private readonly string _tempTableTemplate;
        private readonly string _deleteTemplate;

        private SqlServerDeletePlan(
            string tableName,
            string keyColumn,
            Type keyDeclaredType,
            DataTable schema,
            string tempTableTemplate,
            string deleteTemplate)
        {
            TableName = tableName;
            KeyColumn = keyColumn;
            _keyDeclaredType = keyDeclaredType;
            _schema = schema;
            _tempTableTemplate = tempTableTemplate;
            _deleteTemplate = deleteTemplate;
        }

        public string TableName { get; }
        public string KeyColumn { get; }

        public static SqlServerDeletePlan Create(Type keyType, string tableName, string keyColumn)
        {
            var actual = Nullable.GetUnderlyingType(keyType) ?? keyType;
            if (actual.IsEnum)
                actual = typeof(string);
            if (actual == typeof(DateOnly) || actual == typeof(TimeOnly))
                actual = typeof(string);

            var schema = new DataTable();
            schema.Columns.Add(keyColumn, actual);

            var tempTemplate = $"CREATE TABLE {{0}} ({QuoteIdentifier(keyColumn)} {ToSqlType(keyType)} NULL);";
            var deleteTemplate =
                $"DELETE Target FROM {QuoteTable(tableName)} AS Target INNER JOIN {{0}} AS Source ON Target.{QuoteIdentifier(keyColumn)} = Source.{QuoteIdentifier(keyColumn)};";

            return new SqlServerDeletePlan(tableName, keyColumn, keyType, schema, tempTemplate, deleteTemplate);
        }

        public string CreateTempTableSql(string tempTable) => string.Format(System.Globalization.CultureInfo.InvariantCulture, _tempTableTemplate, tempTable);
        public string CreateDeleteSql(string tempTable) => string.Format(System.Globalization.CultureInfo.InvariantCulture, _deleteTemplate, tempTable);

        public DataTable CreateTable<TKey>(IReadOnlyList<TKey> keys)
        {
            var table = _schema.Clone();
            table.BeginLoadData();

            for (var i = 0; i < keys.Count; i++)
            {
                var row = table.NewRow();
                row[0] = NormalizeValue(keys[i], _keyDeclaredType) ?? DBNull.Value;
                table.Rows.Add(row);
            }

            table.EndLoadData();
            return table;
        }
    }

    private static PropertyInfo[] GetBulkProperties(Type entityType, bool includeIdentity)
    {
        var all = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var selected = new List<PropertyInfo>(all.Length);

        for (var i = 0; i < all.Length; i++)
        {
            var property = all[i];

            if (!property.CanRead || !IsScalar(property.PropertyType))
                continue;

            if (!includeIdentity && IsIdentityConvention(property))
                continue;

            selected.Add(property);
        }

        return selected.ToArray();
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

    private static string[] BuildColumnNames(PropertyInfo[] properties)
    {
        var names = new string[properties.Length];
        for (var i = 0; i < properties.Length; i++)
            names[i] = properties[i].Name;
        return names;
    }

    private static Type[] BuildDeclaredTypes(PropertyInfo[] properties)
    {
        var types = new Type[properties.Length];
        for (var i = 0; i < properties.Length; i++)
            types[i] = properties[i].PropertyType;
        return types;
    }

    private static Func<object, object?>[] BuildGetters(PropertyInfo[] properties)
    {
        var getters = new Func<object, object?>[properties.Length];
        for (var i = 0; i < properties.Length; i++)
            getters[i] = ForgeProviderAccessors.CreateGetter(properties[i]);
        return getters;
    }

    private static DataTable CreateSchema(PropertyInfo[] properties)
    {
        var schema = new DataTable();

        for (var i = 0; i < properties.Length; i++)
        {
            var type = Nullable.GetUnderlyingType(properties[i].PropertyType) ?? properties[i].PropertyType;
            if (type.IsEnum || type == typeof(DateOnly) || type == typeof(TimeOnly))
                type = typeof(string);

            schema.Columns.Add(properties[i].Name, type);
        }

        return schema;
    }

    private static string BuildTempTableSqlTemplate(PropertyInfo[] properties)
    {
        var sql = new System.Text.StringBuilder(256 + properties.Length * 64);
        sql.Append("CREATE TABLE {0} (");

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

    private static string BuildMergeUpdateSqlTemplate(string tableName, string keyColumn, PropertyInfo[] updateProperties)
    {
        var sql = new System.Text.StringBuilder(512 + updateProperties.Length * 64);
        var key = QuoteIdentifier(keyColumn);

        sql.Append("MERGE ").Append(QuoteTable(tableName)).Append(" AS Target ")
           .Append("USING {0} AS Source ")
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


    private static string BuildTvpTypeName(Type entityType)
        => "dbo." + entityType.Name + "TableType";

    private static string BuildInsertFromTvpSql(string tableName, string[] columnNames)
    {
        var columns = string.Join(", ", columnNames.Select(QuoteIdentifier));
        return $"INSERT INTO {QuoteTable(tableName)} ({columns}) SELECT {columns} FROM @Rows;";
    }

    private static SqlMetaData[] BuildSqlMetaData(PropertyInfo[] properties)
    {
        var meta = new SqlMetaData[properties.Length];
        for (var i = 0; i < properties.Length; i++)
            meta[i] = CreateSqlMetaData(properties[i].Name, properties[i].PropertyType);
        return meta;
    }

    private static SqlMetaData CreateSqlMetaData(string name, Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;

        if (actual.IsEnum) return new SqlMetaData(name, SqlDbType.NVarChar, 128);
        if (actual == typeof(string)) return new SqlMetaData(name, SqlDbType.NVarChar, -1);
        if (actual == typeof(int)) return new SqlMetaData(name, SqlDbType.Int);
        if (actual == typeof(long)) return new SqlMetaData(name, SqlDbType.BigInt);
        if (actual == typeof(short)) return new SqlMetaData(name, SqlDbType.SmallInt);
        if (actual == typeof(byte)) return new SqlMetaData(name, SqlDbType.TinyInt);
        if (actual == typeof(bool)) return new SqlMetaData(name, SqlDbType.Bit);
        if (actual == typeof(decimal)) return new SqlMetaData(name, SqlDbType.Decimal, 38, 10);
        if (actual == typeof(float)) return new SqlMetaData(name, SqlDbType.Real);
        if (actual == typeof(double)) return new SqlMetaData(name, SqlDbType.Float);
        if (actual == typeof(DateTime)) return new SqlMetaData(name, SqlDbType.DateTime2);
        if (actual == typeof(DateTimeOffset)) return new SqlMetaData(name, SqlDbType.DateTimeOffset);
        if (actual == typeof(Guid)) return new SqlMetaData(name, SqlDbType.UniqueIdentifier);
        if (actual == typeof(byte[])) return new SqlMetaData(name, SqlDbType.VarBinary, -1);
        if (actual == typeof(TimeSpan)) return new SqlMetaData(name, SqlDbType.Time);
        if (actual == typeof(DateOnly) || actual == typeof(TimeOnly)) return new SqlMetaData(name, SqlDbType.NVarChar, 32);

        return new SqlMetaData(name, SqlDbType.NVarChar, -1);
    }

    private static void SetRecordValue(SqlDataRecord record, int ordinal, object? value, Type declaredType)
    {
        if (value is null || value is DBNull)
        {
            record.SetDBNull(ordinal);
            return;
        }

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual.IsEnum)
        {
            record.SetString(ordinal, value.ToString()!);
            return;
        }

        if (actual == typeof(int)) { record.SetInt32(ordinal, Convert.ToInt32(value)); return; }
        if (actual == typeof(long)) { record.SetInt64(ordinal, Convert.ToInt64(value)); return; }
        if (actual == typeof(short)) { record.SetInt16(ordinal, Convert.ToInt16(value)); return; }
        if (actual == typeof(byte)) { record.SetByte(ordinal, Convert.ToByte(value)); return; }
        if (actual == typeof(bool)) { record.SetBoolean(ordinal, Convert.ToBoolean(value)); return; }
        if (actual == typeof(decimal)) { record.SetDecimal(ordinal, Convert.ToDecimal(value)); return; }
        if (actual == typeof(float)) { record.SetFloat(ordinal, Convert.ToSingle(value)); return; }
        if (actual == typeof(double)) { record.SetDouble(ordinal, Convert.ToDouble(value)); return; }
        if (actual == typeof(DateTime)) { record.SetDateTime(ordinal, Convert.ToDateTime(value)); return; }
        if (actual == typeof(Guid)) { record.SetGuid(ordinal, value is Guid g ? g : Guid.Parse(value.ToString()!)); return; }
        if (actual == typeof(string)) { record.SetString(ordinal, value.ToString()!); return; }
        if (actual == typeof(byte[])) { record.SetBytes(ordinal, 0, (byte[])value, 0, ((byte[])value).Length); return; }

        record.SetValue(ordinal, value);
    }

    private static class SqlServerBulkFallbackPolicy
    {
        public static bool CanFallback(Exception ex)
            => ex is TypeLoadException
                or MissingMethodException
                or InvalidCastException
                or NotSupportedException;
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
