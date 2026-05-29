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
                    cancellationToken).ConfigureAwait(false);

                break;

            case ForgeBulkOperationStrategy.SqlBulkCopy:

                await InsertWithSqlBulkCopyDataReaderAsync(
                    (SqlConnection)connection,
                    plan,
                    list,
                    options,
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

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        switch (options.SqlServerStrategy)
        {
            case ForgeBulkOperationStrategy.SqlBulkCopy:
                return await UpdateWithSqlBulkCopyTempTableAsync(sqlConnection, plan, rows, options, cancellationToken).ConfigureAwait(false);

            case ForgeBulkOperationStrategy.TableTypeParameter:
                return await UpdateWithDataTableTableTypeParameterAsync(sqlConnection, plan, rows, options, cancellationToken).ConfigureAwait(false);

            case ForgeBulkOperationStrategy.SqlDataRecord:
            default:
                return await UpdateWithSqlDataRecordTvpAsync(sqlConnection, plan, rows, options, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async ValueTask<int> BulkDeleteAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        ForgeProviderBulkOptions options,
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

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        switch (options.SqlServerStrategy)
        {
            case ForgeBulkOperationStrategy.SqlBulkCopy:
                return await DeleteWithSqlBulkCopyTempTableAsync(sqlConnection, plan, keys, options, cancellationToken).ConfigureAwait(false);

            case ForgeBulkOperationStrategy.TableTypeParameter:
                return await DeleteWithDataTableTableTypeParameterAsync(sqlConnection, plan, keys, options, cancellationToken).ConfigureAwait(false);

            case ForgeBulkOperationStrategy.SqlDataRecord:
            default:
                return await DeleteWithSqlDataRecordTvpAsync(sqlConnection, plan, keys, options, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<int> UpdateWithSqlDataRecordTvpAsync<T>(
        SqlConnection connection,
        SqlServerUpdatePlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken)
    {
        var typeName = CreateTransientSqlServerTypeName(plan.QuotedTableName, "Update");

        await CreateTableTypeAsync(connection, typeName, plan.Properties, cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = plan.CreateMergeSql("@Rows");
            command.CommandTimeout = options.CommandTimeoutSeconds;

            var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
            parameter.TypeName = typeName;
            parameter.Value = new SqlServerRowRecordRows<T>(rows, plan);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DropTableTypeAsync(connection, typeName, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<int> UpdateWithDataTableTableTypeParameterAsync<T>(
        SqlConnection connection,
        SqlServerUpdatePlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken)
    {
        var typeName = CreateTransientSqlServerTypeName(plan.QuotedTableName, "Update");

        await CreateTableTypeAsync(connection, typeName, plan.Properties, cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = plan.CreateMergeSql("@Rows");
            command.CommandTimeout = options.CommandTimeoutSeconds;

            var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
            parameter.TypeName = typeName;
            parameter.Value = plan.CreateTable(rows);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DropTableTypeAsync(connection, typeName, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<int> UpdateWithSqlBulkCopyTempTableAsync<T>(
        SqlConnection sqlConnection,
        SqlServerUpdatePlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken)
    {
        var tempTable = "#ForgeBulkUpdate_" + Guid.NewGuid().ToString("N");

        await using var create = sqlConnection.CreateCommand();
        create.CommandText = plan.CreateTempTableSql(tempTable);
        create.CommandTimeout = options.CommandTimeoutSeconds;
        await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        using (var bulk = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock, externalTransaction: null)
        {
            DestinationTableName = tempTable,
            BatchSize = options.BatchSize > 0 ? options.BatchSize : Math.Min(Math.Max(rows.Count, 1), 5000),
            BulkCopyTimeout = options.CommandTimeoutSeconds,
            EnableStreaming = true
        })
        {
            for (var i = 0; i < plan.ColumnNames.Length; i++)
                bulk.ColumnMappings.Add(plan.ColumnNames[i], plan.ColumnNames[i]);

            using var reader = new ForgeObjectDataReader<T>(rows, plan);
            await bulk.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await using var merge = sqlConnection.CreateCommand();
            merge.CommandText = plan.CreateMergeSql(tempTable);
            merge.CommandTimeout = options.CommandTimeoutSeconds;
            return await merge.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DropTempTableAsync(sqlConnection, tempTable, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<int> DeleteWithSqlDataRecordTvpAsync<TKey>(
        SqlConnection connection,
        SqlServerDeletePlan plan,
        IReadOnlyList<TKey> keys,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken)
    {
        var typeName = CreateTransientSqlServerTypeName(plan.TableName, "Delete");

        await CreateTableTypeAsync(connection, typeName, plan.KeyColumn, plan.KeyDeclaredType, cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = plan.CreateDeleteSql("@Rows");
            command.CommandTimeout = options.CommandTimeoutSeconds;

            var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
            parameter.TypeName = typeName;
            parameter.Value = new SqlServerKeyRecordRows<TKey>(keys, plan.KeyColumn, plan.KeyDeclaredType);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DropTableTypeAsync(connection, typeName, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<int> DeleteWithDataTableTableTypeParameterAsync<TKey>(
        SqlConnection connection,
        SqlServerDeletePlan plan,
        IReadOnlyList<TKey> keys,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken)
    {
        var typeName = CreateTransientSqlServerTypeName(plan.TableName, "Delete");

        await CreateTableTypeAsync(connection, typeName, plan.KeyColumn, plan.KeyDeclaredType, cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = plan.CreateDeleteSql("@Rows");
            command.CommandTimeout = options.CommandTimeoutSeconds;

            var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
            parameter.TypeName = typeName;
            parameter.Value = plan.CreateTable(keys);

            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DropTableTypeAsync(connection, typeName, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<int> DeleteWithSqlBulkCopyTempTableAsync<TKey>(
        SqlConnection sqlConnection,
        SqlServerDeletePlan plan,
        IReadOnlyList<TKey> keys,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken)
    {
        var tempTable = "#ForgeBulkDelete_" + Guid.NewGuid().ToString("N");

        await using var create = sqlConnection.CreateCommand();
        create.CommandText = plan.CreateTempTableSql(tempTable);
        create.CommandTimeout = options.CommandTimeoutSeconds;
        await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        using (var bulk = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.TableLock, externalTransaction: null)
        {
            DestinationTableName = tempTable,
            BatchSize = options.BatchSize > 0 ? options.BatchSize : Math.Min(Math.Max(keys.Count, 1), 5000),
            BulkCopyTimeout = options.CommandTimeoutSeconds,
            EnableStreaming = true
        })
        {
            bulk.ColumnMappings.Add(plan.KeyColumn, plan.KeyColumn);
            using var reader = new ForgeKeyDataReader<TKey>(keys, plan.KeyColumn, plan.KeyDeclaredType);
            await bulk.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await using var delete = sqlConnection.CreateCommand();
            delete.CommandText = plan.CreateDeleteSql(tempTable);
            delete.CommandTimeout = options.CommandTimeoutSeconds;
            return await delete.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DropTempTableAsync(sqlConnection, tempTable, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask InsertWithSqlBulkCopyDataReaderAsync<T>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken)
    {
        using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock, externalTransaction: null)
        {
            DestinationTableName = plan.QuotedTableName,
            BatchSize = options.BatchSize > 0 ? options.BatchSize : Math.Min(Math.Max(rows.Count, 1), 5000),
            BulkCopyTimeout = options.CommandTimeoutSeconds,
            EnableStreaming = true
        };

        for (var i = 0; i < plan.ColumnNames.Length; i++)
            bulk.ColumnMappings.Add(plan.ColumnNames[i], plan.ColumnNames[i]);

        using var reader = new ForgeObjectDataReader<T>(rows, plan);
        await bulk.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask InsertWithSqlDataRecordTvpAsync<T>(
       SqlConnection connection,
       SqlServerInsertPlan plan,
       IReadOnlyList<T> rows,
       CancellationToken cancellationToken)
    {
        var typeName = CreateTransientSqlServerTypeName(plan.QuotedTableName, "Insert");

        await CreateTableTypeAsync(connection, typeName, plan.Properties, cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = plan.InsertSql;

            var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
            parameter.TypeName = typeName;
            parameter.Value = new SqlDataRecordRows<T>(rows, plan);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DropTableTypeAsync(connection, typeName, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask InsertWithDataTableFallbackAsync<T>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken)
    {
        var typeName = CreateTransientSqlServerTypeName(plan.QuotedTableName, "Insert");

        await CreateTableTypeAsync(connection, typeName, plan.Properties, cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = plan.InsertSql;

            var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
            parameter.TypeName = typeName;
            parameter.Value = plan.CreateTable(rows);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DropTableTypeAsync(connection, typeName, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ForgeObjectDataReader<T> : DbDataReader
    {
        private readonly IReadOnlyList<T> _rows;
        private readonly IForgeBulkRecordPlan _plan;
        private int _index = -1;
        private bool _closed;

        public ForgeObjectDataReader(IReadOnlyList<T> rows, IForgeBulkRecordPlan plan)
        {
            _rows = rows;
            _plan = plan;
        }

        public override int FieldCount => _plan.ColumnNames.Length;
        public override bool HasRows => _rows.Count > 0;
        public override bool IsClosed => _closed;
        public override int RecordsAffected => -1;
        public override int Depth => 0;
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));

        public override bool Read()
        {
            var next = _index + 1;
            if (next >= _rows.Count)
                return false;

            _index = next;
            return true;
        }

        public override bool NextResult() => false;
        public override string GetName(int ordinal) => _plan.ColumnNames[ordinal];
        public override int GetOrdinal(string name)
        {
            for (var i = 0; i < _plan.ColumnNames.Length; i++)
            {
                if (string.Equals(_plan.ColumnNames[i], name, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        public override Type GetFieldType(int ordinal)
        {
            var type = Nullable.GetUnderlyingType(_plan.GetDeclaredType(ordinal)) ?? _plan.GetDeclaredType(ordinal);
            if (type.IsEnum || type == typeof(DateOnly) || type == typeof(TimeOnly))
                return typeof(string);
            return type;
        }

        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
        public override object GetValue(int ordinal)
        {
            if (_index < 0 || _index >= _rows.Count)
                throw new InvalidOperationException("The data reader is not positioned on a row.");

            var value = NormalizeValue(_plan.GetValue(_rows[_index]!, ordinal), _plan.GetDeclaredType(ordinal));
            return value ?? DBNull.Value;
        }

        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; i++)
                values[i] = GetValue(i);
            return count;
        }

        public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;
        public override bool GetBoolean(int ordinal) => Convert.ToBoolean(GetValue(ordinal));
        public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal));
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        {
            var bytes = (byte[])GetValue(ordinal);
            var available = Math.Max(0, bytes.Length - (int)dataOffset);
            var count = Math.Min(length, available);
            if (buffer is not null && count > 0)
                Array.Copy(bytes, (int)dataOffset, buffer, bufferOffset, count);
            return count;
        }
        public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal));
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            var chars = Convert.ToString(GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture)?.ToCharArray() ?? Array.Empty<char>();
            var available = Math.Max(0, chars.Length - (int)dataOffset);
            var count = Math.Min(length, available);
            if (buffer is not null && count > 0)
                Array.Copy(chars, (int)dataOffset, buffer, bufferOffset, count);
            return count;
        }
        public override Guid GetGuid(int ordinal) => GetValue(ordinal) is Guid g ? g : Guid.Parse(Convert.ToString(GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture)!);
        public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
        public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
        public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
        public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
        public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
        public override string GetString(int ordinal) => Convert.ToString(GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture)!;
        public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
        public override DateTime GetDateTime(int ordinal) => Convert.ToDateTime(GetValue(ordinal));
        public override IEnumerator GetEnumerator() { while (Read()) yield return this; }
        public override void Close() => _closed = true;
    }

    private sealed class ForgeKeyDataReader<TKey> : DbDataReader
    {
        private readonly IReadOnlyList<TKey> _keys;
        private readonly string _keyColumn;
        private readonly Type _keyType;
        private int _index = -1;
        private bool _closed;

        public ForgeKeyDataReader(IReadOnlyList<TKey> keys, string keyColumn, Type keyType)
        {
            _keys = keys;
            _keyColumn = keyColumn;
            _keyType = keyType;
        }

        public override int FieldCount => 1;
        public override bool HasRows => _keys.Count > 0;
        public override bool IsClosed => _closed;
        public override int RecordsAffected => -1;
        public override int Depth => 0;
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));
        public override bool Read() { var next = _index + 1; if (next >= _keys.Count) return false; _index = next; return true; }
        public override bool NextResult() => false;
        public override string GetName(int ordinal) => _keyColumn;
        public override int GetOrdinal(string name) => string.Equals(name, _keyColumn, StringComparison.OrdinalIgnoreCase) ? 0 : -1;
        public override Type GetFieldType(int ordinal) => Nullable.GetUnderlyingType(_keyType) ?? _keyType;
        public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
        public override object GetValue(int ordinal) => NormalizeValue(_keys[_index], _keyType) ?? DBNull.Value;
        public override int GetValues(object[] values) { if (values.Length > 0) values[0] = GetValue(0); return Math.Min(values.Length, 1); }
        public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;
        public override bool GetBoolean(int ordinal) => Convert.ToBoolean(GetValue(ordinal));
        public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal));
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
        public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal));
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
        public override Guid GetGuid(int ordinal) => GetValue(ordinal) is Guid g ? g : Guid.Parse(GetString(ordinal));
        public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
        public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
        public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
        public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
        public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
        public override string GetString(int ordinal) => Convert.ToString(GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture)!;
        public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
        public override DateTime GetDateTime(int ordinal) => Convert.ToDateTime(GetValue(ordinal));
        public override IEnumerator GetEnumerator() { while (Read()) yield return this; }
        public override void Close() => _closed = true;
    }

    private interface IForgeBulkRecordPlan
    {
        string[] ColumnNames { get; }
        object? GetValue(object entity, int ordinal);
        Type GetDeclaredType(int ordinal);
    }

    private sealed class SqlServerInsertPlan : IForgeBulkRecordPlan
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

    private sealed class SqlServerRowRecordRows<T> : IEnumerable<SqlDataRecord>
    {
        private readonly IReadOnlyList<T> _rows;
        private readonly SqlServerUpdatePlan _plan;

        public SqlServerRowRecordRows(IReadOnlyList<T> rows, SqlServerUpdatePlan plan)
        {
            _rows = rows;
            _plan = plan;
        }

        public IEnumerator<SqlDataRecord> GetEnumerator()
        {
            var metadata = BuildSqlMetaData(_plan.Properties);

            for (var r = 0; r < _rows.Count; r++)
            {
                var record = new SqlDataRecord(metadata);
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

    private sealed class SqlServerKeyRecordRows<TKey> : IEnumerable<SqlDataRecord>
    {
        private readonly IReadOnlyList<TKey> _keys;
        private readonly string _keyColumn;
        private readonly Type _keyType;

        public SqlServerKeyRecordRows(IReadOnlyList<TKey> keys, string keyColumn, Type keyType)
        {
            _keys = keys;
            _keyColumn = keyColumn;
            _keyType = keyType;
        }

        public IEnumerator<SqlDataRecord> GetEnumerator()
        {
            var metadata = new[] { CreateSqlMetaData(_keyColumn, _keyType) };

            for (var i = 0; i < _keys.Count; i++)
            {
                var record = new SqlDataRecord(metadata);
                var value = NormalizeValue(_keys[i], _keyType);
                SetRecordValue(record, 0, value, _keyType);
                yield return record;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class SqlServerUpdatePlan : IForgeBulkRecordPlan
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
        public Type KeyDeclaredType => _keyDeclaredType;

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


    private static async ValueTask CreateTableTypeAsync(
        SqlConnection connection,
        string typeName,
        PropertyInfo[] properties,
        CancellationToken cancellationToken)
    {
        var sql = new System.Text.StringBuilder(256 + properties.Length * 64);
        sql.Append("CREATE TYPE ").Append(typeName).Append(" AS TABLE (");

        for (var i = 0; i < properties.Length; i++)
        {
            if (i > 0)
                sql.Append(", ");

            sql.Append(QuoteIdentifier(properties[i].Name))
               .Append(' ')
               .Append(ToSqlType(properties[i].PropertyType))
               .Append(" NULL");
        }

        sql.Append(");");

        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask CreateTableTypeAsync(
        SqlConnection connection,
        string typeName,
        string keyColumn,
        Type keyType,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE TYPE {typeName} AS TABLE ({QuoteIdentifier(keyColumn)} {ToSqlType(keyType)} NULL);";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask DropTempTableAsync(
        SqlConnection connection,
        string tempTable,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE IF EXISTS " + tempTable + ";";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException)
        {
            // Best effort cleanup.
        }
    }

    private static async ValueTask DropTableTypeAsync(
        SqlConnection connection,
        string typeName,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "DROP TYPE " + typeName + ";";
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException)
        {
            // Best effort cleanup. The bulk operation result must not be hidden by cleanup failure.
        }
    }

    private static string CreateTransientSqlServerTypeName(string tableName, string operation)
    {
        var clean = tableName
            .Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal)
            .Replace(".", "_", StringComparison.Ordinal);

        return "dbo.ForgeBulk_" + operation + "_" + clean + "_" + Guid.NewGuid().ToString("N");
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
