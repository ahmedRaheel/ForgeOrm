using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using ForgeORM.Core.Graph;

namespace ForgeORM.Core;

/// <summary>
/// Provider-neutral bulk fallback. This class must never use row-by-row INSERT/UPDATE/DELETE loops.
/// Provider packages should use their native optimized path first (SQL Server TVP/SqlDataRecord, PostgreSQL COPY,
/// MySQL multi-row/temp table, Oracle array binding). If the native path is unavailable, this fallback still sends
/// batched commands instead of per-row SQL.
/// </summary>
internal static class ForgeProviderBulkFallback
{
    private const int DefaultBatchSize = 250;
    private const int SqlServerMaxParameters = 2000;

    private static readonly ConcurrentDictionary<(Type Type, string Table), BulkShape> InsertShapeCache = new();
    private static readonly ConcurrentDictionary<(Type Type, string Table, string Key), BulkShape> UpdateShapeCache = new();

    public static ValueTask<int> InsertRowsAsync<T>(
        DbConnection connection,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        var metadata = ForgeEntityMetadataCache.Get(typeof(T));
        return InsertRowsAsync(connection, metadata.TableName, rows, cancellationToken);
    }

    public static async ValueTask<int> InsertRowsAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);

        if (rows.Count == 0)
            return 0;

        var shape = InsertShapeCache.GetOrAdd((typeof(T), tableName), static key => CreateShape(key.Type, key.Table, keyColumn: null, includeKey: false));
        if (shape.Columns.Length == 0)
            return 0;

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var batchSize = GetBatchSize(connection, shape.Columns.Length);
        var affected = 0;

        for (var offset = 0; offset < rows.Count; offset += batchSize)
        {
            var count = Math.Min(batchSize, rows.Count - offset);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = BuildInsertSql(connection, shape, count);
            AddInsertParameters(command, shape, rows, offset, count);
            affected += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return affected;
    }

    public static ValueTask<int> UpdateRowAsync<T>(
        DbConnection connection,
        string tableName,
        T entity,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (entity is null)
            return ValueTask.FromResult(0);

        return UpdateRowsAsync(connection, tableName, new[] { entity }, keyColumn, cancellationToken);
    }

    public static async ValueTask<int> UpdateRowsAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyColumn);

        if (rows.Count == 0)
            return 0;

        var shape = UpdateShapeCache.GetOrAdd((typeof(T), tableName, keyColumn), static key => CreateShape(key.Type, key.Table, key.Key, includeKey: true));
        if (shape.Key is null || shape.Columns.Length == 0)
            return 0;

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var valuesPerRow = shape.Columns.Length + 1;
        var batchSize = GetBatchSize(connection, valuesPerRow);
        var affected = 0;

        for (var offset = 0; offset < rows.Count; offset += batchSize)
        {
            var count = Math.Min(batchSize, rows.Count - offset);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = BuildUpdateSql(connection, shape, count);
            AddUpdateParameters(command, shape, rows, offset, count);
            affected += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return affected;
    }

    public static async ValueTask<int> DeleteRowsAsync<TKey>(
        DbConnection connection,
        string tableName,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyColumn);

        if (keys.Count == 0)
            return 0;

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var batchSize = GetBatchSize(connection, valuesPerRow: 1);
        var affected = 0;

        for (var offset = 0; offset < keys.Count; offset += batchSize)
        {
            var count = Math.Min(batchSize, keys.Count - offset);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = BuildDeleteSql(connection, tableName, keyColumn, count);
            AddDeleteParameters(command, keys, offset, count);
            affected += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return affected;
    }

    private static BulkShape CreateShape(Type type, string tableName, string? keyColumn, bool includeKey)
    {
        var metadata = ForgeEntityMetadataCache.Get(type);
        var key = FindKey(metadata, keyColumn);
        var props = metadata.ScalarProperties;
        var columns = new List<BulkColumn>(props.Count);

        for (var i = 0; i < props.Count; i++)
        {
            var property = props[i];
            if (!property.CanRead)
                continue;

            if (!includeKey && key is not null && string.Equals(property.Name, key.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (includeKey && key is not null && string.Equals(property.Name, key.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            columns.Add(new BulkColumn(property.Name, property.PropertyType, property));
        }

        return new BulkShape(tableName, key?.Name ?? keyColumn, key is null ? null : new BulkColumn(key.Name, key.PropertyType, key), columns.ToArray());
    }

    private static PropertyInfo? FindKey(ForgeEntityMetadata metadata, string? keyColumn)
    {
        if (!string.IsNullOrWhiteSpace(keyColumn))
        {
            for (var i = 0; i < metadata.ScalarProperties.Count; i++)
            {
                if (string.Equals(metadata.ScalarProperties[i].Name, keyColumn, StringComparison.OrdinalIgnoreCase))
                    return metadata.ScalarProperties[i];
            }
        }

        return metadata.KeyProperty;
    }

    private static string BuildInsertSql(DbConnection connection, BulkShape shape, int rowCount)
    {
        var p = ParameterPrefix(connection);
        var sql = new StringBuilder(shape.TableName.Length + (shape.Columns.Length * rowCount * 14) + 128);

        sql.Append("INSERT INTO ").Append(QuoteTable(connection, shape.TableName)).Append(" (");
        AppendColumnList(sql, connection, shape.Columns);
        sql.Append(") VALUES ");

        for (var r = 0; r < rowCount; r++)
        {
            if (r > 0)
                sql.Append(", ");

            sql.Append('(');
            for (var c = 0; c < shape.Columns.Length; c++)
            {
                if (c > 0)
                    sql.Append(", ");

                sql.Append(p).Append('p').Append(r).Append('_').Append(c);
            }
            sql.Append(')');
        }

        return sql.ToString();
    }

    private static string BuildUpdateSql(DbConnection connection, BulkShape shape, int rowCount)
    {
        if (shape.Key is null)
            throw new InvalidOperationException($"Bulk update requires a key column for table '{shape.TableName}'.");

        var p = ParameterPrefix(connection);
        var sql = new StringBuilder(shape.TableName.Length + (shape.Columns.Length * rowCount * 28) + 256);
        var quotedKey = QuoteIdentifier(connection, shape.Key.ColumnName);

        sql.Append("UPDATE ").Append(QuoteTable(connection, shape.TableName)).Append(" SET ");

        for (var c = 0; c < shape.Columns.Length; c++)
        {
            if (c > 0)
                sql.Append(", ");

            sql.Append(QuoteIdentifier(connection, shape.Columns[c].ColumnName)).Append(" = CASE ").Append(quotedKey).Append(' ');

            for (var r = 0; r < rowCount; r++)
            {
                sql.Append("WHEN ").Append(p).Append('k').Append(r)
                   .Append(" THEN ").Append(p).Append('p').Append(r).Append('_').Append(c).Append(' ');
            }

            sql.Append("ELSE ").Append(QuoteIdentifier(connection, shape.Columns[c].ColumnName)).Append(" END");
        }

        sql.Append(" WHERE ").Append(quotedKey).Append(" IN (");
        for (var r = 0; r < rowCount; r++)
        {
            if (r > 0)
                sql.Append(", ");
            sql.Append(p).Append('k').Append(r);
        }
        sql.Append(')');

        return sql.ToString();
    }

    private static string BuildDeleteSql(DbConnection connection, string tableName, string keyColumn, int rowCount)
    {
        var p = ParameterPrefix(connection);
        var sql = new StringBuilder(tableName.Length + (rowCount * 8) + 64);

        sql.Append("DELETE FROM ").Append(QuoteTable(connection, tableName))
           .Append(" WHERE ").Append(QuoteIdentifier(connection, keyColumn)).Append(" IN (");

        for (var i = 0; i < rowCount; i++)
        {
            if (i > 0)
                sql.Append(", ");
            sql.Append(p).Append('k').Append(i);
        }

        sql.Append(')');
        return sql.ToString();
    }

    private static void AddInsertParameters<T>(DbCommand command, BulkShape shape, IReadOnlyList<T> rows, int offset, int count)
    {
        for (var r = 0; r < count; r++)
        {
            var item = rows[offset + r]!;
            for (var c = 0; c < shape.Columns.Length; c++)
            {
                AddParameter(command, "p" + r + "_" + c, NormalizeValue(shape.Columns[c].Property.GetValue(item), shape.Columns[c].ClrType));
            }
        }
    }

    private static void AddUpdateParameters<T>(DbCommand command, BulkShape shape, IReadOnlyList<T> rows, int offset, int count)
    {
        if (shape.Key is null)
            return;

        for (var r = 0; r < count; r++)
        {
            var item = rows[offset + r]!;
            AddParameter(command, "k" + r, NormalizeValue(shape.Key.Property.GetValue(item), shape.Key.ClrType));

            for (var c = 0; c < shape.Columns.Length; c++)
            {
                AddParameter(command, "p" + r + "_" + c, NormalizeValue(shape.Columns[c].Property.GetValue(item), shape.Columns[c].ClrType));
            }
        }
    }

    private static void AddDeleteParameters<TKey>(DbCommand command, IReadOnlyList<TKey> keys, int offset, int count)
    {
        for (var i = 0; i < count; i++)
            AddParameter(command, "k" + i, keys[offset + i]);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = ParameterPrefix(command.Connection!) + name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static object? NormalizeValue(object? value, Type? declaredType = null)
    {
        if (value is null)
            return null;

        var actual = declaredType is null ? value.GetType() : Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual.IsEnum)
            return value.ToString();

        if (actual == typeof(DateTime) && value is DateTime dt)
            return dt == default || dt < new DateTime(1753, 1, 1) ? DateTime.UtcNow : dt;

        if (actual == typeof(DateTimeOffset) && value is DateTimeOffset dto)
            return dto == default ? DateTimeOffset.UtcNow : dto;

        return value;
    }

    private static int GetBatchSize(DbConnection connection, int valuesPerRow)
    {
        if (valuesPerRow <= 0)
            return DefaultBatchSize;

        var provider = connection.GetType().FullName ?? string.Empty;
        var maxParameters = provider.Contains("SqlClient", StringComparison.OrdinalIgnoreCase)
            ? SqlServerMaxParameters
            : 5000;

        var byParameters = Math.Max(1, maxParameters / valuesPerRow);
        return Math.Min(DefaultBatchSize, byParameters);
    }

    private static string ParameterPrefix(DbConnection connection)
    {
        var provider = connection.GetType().FullName ?? string.Empty;
        return provider.Contains("Oracle", StringComparison.OrdinalIgnoreCase) ? ":" : "@";
    }

    private static void AppendColumnList(StringBuilder sql, DbConnection connection, BulkColumn[] columns)
    {
        for (var i = 0; i < columns.Length; i++)
        {
            if (i > 0)
                sql.Append(", ");
            sql.Append(QuoteIdentifier(connection, columns[i].ColumnName));
        }
    }

    private static string QuoteTable(DbConnection connection, string tableName)
    {
        var parts = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
            return QuoteIdentifier(connection, parts[0]);

        var builder = new StringBuilder(tableName.Length + (parts.Length * 2));
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0)
                builder.Append('.');
            builder.Append(QuoteIdentifier(connection, parts[i]));
        }
        return builder.ToString();
    }

    private static string QuoteIdentifier(DbConnection connection, string identifier)
    {
        var provider = connection.GetType().FullName ?? string.Empty;
        var clean = identifier.Trim('[', ']', '`', '"');

        if (provider.Contains("MySql", StringComparison.OrdinalIgnoreCase))
            return "`" + clean.Replace("`", "``", StringComparison.Ordinal) + "`";

        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) || provider.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
            return "\"" + clean.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

        if (provider.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
            return "\"" + clean.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

        return "[" + clean.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }

    private sealed record BulkShape(string TableName, string? KeyColumn, BulkColumn? Key, BulkColumn[] Columns);
    private sealed record BulkColumn(string ColumnName, Type ClrType, PropertyInfo Property);
}
