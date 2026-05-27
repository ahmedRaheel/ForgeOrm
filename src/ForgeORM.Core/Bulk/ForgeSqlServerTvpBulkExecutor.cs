using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using ForgeORM.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace ForgeORM.Core;

/// <summary>
/// SQL Server TVP/MERGE bulk executor. The plan is created once per entity and then reused by
/// InsertBulkAsync, BulkUpdateAsync and BulkDeleteAsync. It avoids row-by-row execution and avoids
/// DataTable/DataRow allocation in the hot path by streaming SqlDataRecord rows to SQL Server.
/// </summary>
internal static class ForgeSqlServerTvpBulkExecutor
{
    private static readonly ConcurrentDictionary<Type, SqlServerBulkEntityPlan> EntityPlans = new();
    private static readonly ConcurrentDictionary<string, byte> EnsuredTypes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, SqlServerBulkKeyPlan> KeyPlans = new(StringComparer.OrdinalIgnoreCase);

    public static ValueTask<int> InsertAsync<T>(DbConnection connection, ForgeEntityMetadata metadata, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        if (connection is not SqlConnection sqlConnection)
            return ForgeProviderBulkFallback.InsertRowsAsync(connection, rows.ToArray(), cancellationToken);

        return InsertSqlServerAsync(sqlConnection, metadata, rows, cancellationToken);
    }

    public static ValueTask<int> InsertAsync<T>(DbConnection connection, IReadOnlyList<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        if (connection is not SqlConnection sqlConnection)
            return ForgeProviderBulkFallback.InsertRowsAsync(connection, rows, cancellationToken);

        var metadata = CreateFallbackMetadata<T>();
        return InsertSqlServerAsync(sqlConnection, metadata, rows, cancellationToken);
    }

    public static ValueTask<int> UpdateAsync<T>(DbConnection connection, ForgeEntityMetadata metadata, IReadOnlyCollection<T> rows, string? keyColumn = null, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        if (connection is not SqlConnection sqlConnection)
            return UpdateFallbackAsync(connection, metadata, rows, keyColumn ?? metadata.KeyColumn, cancellationToken);

        return UpdateSqlServerAsync(sqlConnection, metadata, rows, keyColumn ?? metadata.KeyColumn, cancellationToken);
    }

    public static ValueTask<int> DeleteAsync<TKey>(DbConnection connection, string tableName, string keyColumn, IReadOnlyCollection<TKey> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return ValueTask.FromResult(0);

        if (connection is not SqlConnection sqlConnection)
            return DeleteFallbackAsync(connection, tableName, keyColumn, ids, cancellationToken);

        return DeleteSqlServerAsync(sqlConnection, tableName, keyColumn, ids, cancellationToken);
    }

    private static async ValueTask<int> InsertSqlServerAsync<T>(SqlConnection connection, ForgeEntityMetadata metadata, IReadOnlyCollection<T> rows, CancellationToken cancellationToken)
    {
        var plan = GetEntityPlan(metadata);
        if (plan.InsertColumns.Length == 0)
            return 0;

        await EnsureTableTypeAsync(connection, plan.InsertTypeName, plan.InsertTypeSql, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.InsertSql;
        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.InsertTypeName;
        parameter.Value = new ForgeSqlDataRecordEnumerable<T>(rows, plan.InsertColumns, plan.InsertMetaData);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> UpdateSqlServerAsync<T>(SqlConnection connection, ForgeEntityMetadata metadata, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken)
    {
        var plan = GetEntityPlan(metadata);
        if (plan.UpdateColumns.Length == 0)
            return 0;

        await EnsureTableTypeAsync(connection, plan.UpdateTypeName, plan.UpdateTypeSql, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.UpdateSql;
        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.UpdateTypeName;
        parameter.Value = new ForgeSqlDataRecordEnumerable<T>(rows, plan.UpdateColumns, plan.UpdateMetaData);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> DeleteSqlServerAsync<TKey>(SqlConnection connection, string tableName, string keyColumn, IReadOnlyCollection<TKey> ids, CancellationToken cancellationToken)
    {
        var key = $"{tableName}|{keyColumn}|{typeof(TKey).FullName}";
        var plan = KeyPlans.GetOrAdd(key, _ => BuildKeyPlan(tableName, keyColumn, typeof(TKey)));

        await EnsureTableTypeAsync(connection, plan.TypeName, plan.TypeSql, cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.DeleteSql;
        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TypeName;
        parameter.Value = new ForgeSqlKeyDataRecordEnumerable<TKey>(ids, plan.KeyMetaData);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask EnsureTableTypeAsync(SqlConnection connection, string typeName, string typeSql, CancellationToken cancellationToken)
    {
        if (EnsuredTypes.ContainsKey(typeName))
            return;

        await using var command = connection.CreateCommand();
        command.CommandText = $"IF TYPE_ID(N'{EscapeSqlLiteral(typeName)}') IS NULL EXEC(N'{EscapeSqlLiteral(typeSql)}');";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        EnsuredTypes[typeName] = 1;
    }

    private static SqlServerBulkEntityPlan GetEntityPlan(ForgeEntityMetadata metadata)
        => EntityPlans.GetOrAdd(metadata.EntityType, _ => BuildEntityPlan(metadata));

    private static SqlServerBulkEntityPlan BuildEntityPlan(ForgeEntityMetadata metadata)
    {
        var tableSql = QuoteTableName(metadata.TableName);
        var safeName = SanitizeName(UnqualifiedName(metadata.TableName));
        var insertTypeName = $"dbo.{safeName}_ForgeBulkInsertType";
        var updateTypeName = $"dbo.{safeName}_ForgeBulkUpdateType";

        var properties = metadata.Properties
            .Where(static p => !p.IsComputed)
            .Select(p => CreateColumn(metadata.EntityType, p))
            .ToArray();

        var keyColumn = properties.FirstOrDefault(p => string.Equals(p.ColumnName, metadata.KeyColumn, StringComparison.OrdinalIgnoreCase))
            ?? properties.FirstOrDefault(p => p.IsKey)
            ?? properties.FirstOrDefault(p => string.Equals(p.ColumnName, "Id", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Bulk update/delete requires a key column for {metadata.EntityType.FullName}.");

        // Insert defaults to excluding the key to support identity columns. Non-identity key insert can still use normal Insert.
        var insertColumns = properties.Where(p => !p.IsKey && !string.Equals(p.ColumnName, metadata.KeyColumn, StringComparison.OrdinalIgnoreCase)).ToArray();
        var updateColumns = properties.ToArray();
        var setColumns = updateColumns.Where(p => !string.Equals(p.ColumnName, keyColumn.ColumnName, StringComparison.OrdinalIgnoreCase)).ToArray();

        var insertMeta = CreateMetaData(insertColumns);
        var updateMeta = CreateMetaData(updateColumns);

        var insertTypeSql = BuildCreateTypeSql(insertTypeName, insertColumns);
        var updateTypeSql = BuildCreateTypeSql(updateTypeName, updateColumns);

        var insertSql = BuildInsertSql(tableSql, insertColumns);
        var updateSql = BuildMergeUpdateSql(tableSql, keyColumn, setColumns);

        return new SqlServerBulkEntityPlan(
            metadata.EntityType,
            metadata.TableName,
            insertTypeName,
            updateTypeName,
            insertTypeSql,
            updateTypeSql,
            insertSql,
            updateSql,
            insertColumns,
            updateColumns,
            insertMeta,
            updateMeta);
    }

    private static SqlServerBulkKeyPlan BuildKeyPlan(string tableName, string keyColumn, Type keyType)
    {
        var tableSql = QuoteTableName(tableName);
        var safeName = SanitizeName(UnqualifiedName(tableName));
        var typeName = $"dbo.{safeName}_{SanitizeName(keyColumn)}_ForgeBulkDeleteType";
        var meta = new[] { CreateMetaData(keyColumn, keyType) };
        var typeSql = BuildCreateTypeSql(typeName, keyColumn, keyType);
        var deleteSql = $"DELETE T FROM {tableSql} AS T INNER JOIN @Rows AS S ON T.{QuoteIdentifier(keyColumn)} = S.{QuoteIdentifier(keyColumn)};";
        return new SqlServerBulkKeyPlan(typeName, typeSql, deleteSql, meta);
    }


    private static string BuildInsertSql(string tableSql, IReadOnlyList<SqlServerBulkColumn> columns)
    {
        var colList = string.Join(", ", columns.Select(c => QuoteIdentifier(c.ColumnName)));
        return $"INSERT INTO {tableSql} ({colList}) SELECT {colList} FROM @Rows;";
    }

    private static string BuildMergeUpdateSql(string tableSql, SqlServerBulkColumn keyColumn, IReadOnlyList<SqlServerBulkColumn> setColumns)
    {
        var assignments = string.Join(", ", setColumns.Select(c => $"T.{QuoteIdentifier(c.ColumnName)} = S.{QuoteIdentifier(c.ColumnName)}"));
        return $"MERGE {tableSql} AS T USING @Rows AS S ON T.{QuoteIdentifier(keyColumn.ColumnName)} = S.{QuoteIdentifier(keyColumn.ColumnName)} WHEN MATCHED THEN UPDATE SET {assignments};";
    }

    private static string BuildCreateTypeSql(string typeName, IReadOnlyList<SqlServerBulkColumn> columns)
    {
        var definitions = string.Join(", ", columns.Select(c => $"{QuoteIdentifier(c.ColumnName)} {GetSqlTypeDefinition(c.PropertyType)} NULL"));
        return $"CREATE TYPE {QuoteTypeName(typeName)} AS TABLE ({definitions})";
    }

    private static string BuildCreateTypeSql(string typeName, string columnName, Type columnType)
        => $"CREATE TYPE {QuoteTypeName(typeName)} AS TABLE ({QuoteIdentifier(columnName)} {GetSqlTypeDefinition(columnType)} NOT NULL)";

    private static SqlServerBulkColumn CreateColumn(Type entityType, ForgePropertyMetadata property)
    {
        var propertyInfo = entityType.GetProperty(property.PropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return new SqlServerBulkColumn(
            property.PropertyName,
            property.ColumnName,
            property.PropertyType,
            property.IsKey,
            propertyInfo is null ? null : ForgeIlAccessors.Getter(propertyInfo));
    }

    private static SqlMetaData[] CreateMetaData(IReadOnlyList<SqlServerBulkColumn> columns)
    {
        var metadata = new SqlMetaData[columns.Count];
        for (var i = 0; i < columns.Count; i++)
            metadata[i] = CreateMetaData(columns[i].ColumnName, columns[i].PropertyType);
        return metadata;
    }

    private static SqlMetaData CreateMetaData(string name, Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        if (actual.IsEnum)
            return new SqlMetaData(name, SqlDbType.NVarChar, 128);
        if (actual == typeof(string))
            return new SqlMetaData(name, SqlDbType.NVarChar, SqlMetaData.Max);
        if (actual == typeof(int))
            return new SqlMetaData(name, SqlDbType.Int);
        if (actual == typeof(long))
            return new SqlMetaData(name, SqlDbType.BigInt);
        if (actual == typeof(short))
            return new SqlMetaData(name, SqlDbType.SmallInt);
        if (actual == typeof(byte))
            return new SqlMetaData(name, SqlDbType.TinyInt);
        if (actual == typeof(bool))
            return new SqlMetaData(name, SqlDbType.Bit);
        if (actual == typeof(decimal))
            return new SqlMetaData(name, SqlDbType.Decimal, 38, 10);
        if (actual == typeof(double))
            return new SqlMetaData(name, SqlDbType.Float);
        if (actual == typeof(float))
            return new SqlMetaData(name, SqlDbType.Real);
        if (actual == typeof(DateTime))
            return new SqlMetaData(name, SqlDbType.DateTime2);
        if (actual == typeof(DateTimeOffset))
            return new SqlMetaData(name, SqlDbType.DateTimeOffset);
        if (actual == typeof(Guid))
            return new SqlMetaData(name, SqlDbType.UniqueIdentifier);
        if (actual == typeof(byte[]))
            return new SqlMetaData(name, SqlDbType.VarBinary, SqlMetaData.Max);
        return new SqlMetaData(name, SqlDbType.NVarChar, SqlMetaData.Max);
    }

    private static string GetSqlTypeDefinition(Type type)
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
        if (actual == typeof(double)) return "FLOAT";
        if (actual == typeof(float)) return "REAL";
        if (actual == typeof(DateTime)) return "DATETIME2";
        if (actual == typeof(DateTimeOffset)) return "DATETIMEOFFSET";
        if (actual == typeof(Guid)) return "UNIQUEIDENTIFIER";
        if (actual == typeof(byte[])) return "VARBINARY(MAX)";
        return "NVARCHAR(MAX)";
    }

    private static object? ToSqlValue(object? value, Type type)
    {
        if (value is null)
            return DBNull.Value;

        var actual = Nullable.GetUnderlyingType(type) ?? type;
        if (actual.IsEnum)
            return value.ToString();

        return value;
    }

    private static async ValueTask<int> UpdateFallbackAsync<T>(DbConnection connection, ForgeEntityMetadata metadata, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var row in rows)
        {
            // Keep fallback provider-safe. The optimized path is SQL Server TVP + MERGE.
            affected += await ForgeProviderBulkFallback.UpdateRowAsync(connection, metadata.TableName, row!, keyColumn, cancellationToken).ConfigureAwait(false);
        }
        return affected;
    }

    private static async ValueTask<int> DeleteFallbackAsync<TKey>(DbConnection connection, string tableName, string keyColumn, IReadOnlyCollection<TKey> ids, CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var id in ids)
            affected += await ForgeProviderBulkFallback.DeleteRowAsync(connection, tableName, keyColumn, id!, cancellationToken).ConfigureAwait(false);
        return affected;
    }

    private static ForgeEntityMetadata CreateFallbackMetadata<T>()
    {
        var graph = Graph.ForgeEntityMetadataCache.Get(typeof(T));
        return new ForgeEntityMetadata
        {
            EntityType = typeof(T),
            TableName = graph.TableName,
            KeyColumn = graph.KeyProperty?.Name ?? "Id",
            Properties = graph.ScalarProperties.Select(p => new ForgePropertyMetadata
            {
                PropertyName = p.Name,
                ColumnName = p.Name,
                PropertyType = p.PropertyType,
                IsKey = graph.KeyProperty is not null && string.Equals(p.Name, graph.KeyProperty.Name, StringComparison.OrdinalIgnoreCase)
            }).ToArray()
        };
    }

    private static string QuoteTableName(string tableName)
        => string.Join('.', tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(QuoteIdentifier));

    private static string QuoteTypeName(string typeName)
        => string.Join('.', typeName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(QuoteIdentifier));

    private static string QuoteIdentifier(string name)
        => "[" + name.Trim('[', ']').Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string UnqualifiedName(string tableName)
    {
        var parts = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? tableName : parts[^1].Trim('[', ']');
    }

    private static string SanitizeName(string value)
    {
        var chars = value.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray();
        return new string(chars);
    }

    private static string EscapeSqlLiteral(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);

    private sealed record SqlServerBulkEntityPlan(
        Type EntityType,
        string TableName,
        string InsertTypeName,
        string UpdateTypeName,
        string InsertTypeSql,
        string UpdateTypeSql,
        string InsertSql,
        string UpdateSql,
        SqlServerBulkColumn[] InsertColumns,
        SqlServerBulkColumn[] UpdateColumns,
        SqlMetaData[] InsertMetaData,
        SqlMetaData[] UpdateMetaData);

    private sealed record SqlServerBulkKeyPlan(string TypeName, string TypeSql, string DeleteSql, SqlMetaData[] KeyMetaData);

    private sealed record SqlServerBulkColumn(string PropertyName, string ColumnName, Type PropertyType, bool IsKey, Func<object, object?>? Getter);

    private sealed class ForgeSqlDataRecordEnumerable<T> : IEnumerable<SqlDataRecord>
    {
        private readonly IReadOnlyCollection<T> _rows;
        private readonly SqlServerBulkColumn[] _columns;
        private readonly SqlMetaData[] _metadata;

        public ForgeSqlDataRecordEnumerable(IReadOnlyCollection<T> rows, SqlServerBulkColumn[] columns, SqlMetaData[] metadata)
        {
            _rows = rows;
            _columns = columns;
            _metadata = metadata;
        }

        public IEnumerator<SqlDataRecord> GetEnumerator()
        {
            foreach (var row in _rows)
            {
                var record = new SqlDataRecord(_metadata);
                var boxed = (object?)row;
                for (var i = 0; i < _columns.Length; i++)
                {
                    var column = _columns[i];
                    var value = boxed is null || column.Getter is null ? null : column.Getter(boxed);
                    record.SetValue(i, ToSqlValue(value, column.PropertyType));
                }
                yield return record;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ForgeSqlKeyDataRecordEnumerable<TKey> : IEnumerable<SqlDataRecord>
    {
        private readonly IReadOnlyCollection<TKey> _ids;
        private readonly SqlMetaData[] _metadata;

        public ForgeSqlKeyDataRecordEnumerable(IReadOnlyCollection<TKey> ids, SqlMetaData[] metadata)
        {
            _ids = ids;
            _metadata = metadata;
        }

        public IEnumerator<SqlDataRecord> GetEnumerator()
        {
            foreach (var id in _ids)
            {
                var record = new SqlDataRecord(_metadata);
                record.SetValue(0, id is null ? DBNull.Value : id);
                yield return record;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
