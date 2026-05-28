using ForgeORM.Abstractions;
using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static ForgeORM.Providers.MySql.ForgeProviderAdo;

namespace ForgeORM.Providers.MySql;

public sealed class MySqlForgeProvider : IForgeDatabaseProvider
{
    public string ProviderName => "MySql";

    public ForgeSqlDialect Dialect { get; } = new()
    {
        Name = "MySql",
        ParameterPrefix = "@",
        OpenIdentifier = "`",
        CloseIdentifier = "`"
    };

    public ForgeProviderCapabilities Capabilities { get; } = new()
    {
        SupportsBulkInsert = true,
        SupportsBulkUpdate = true,
        SupportsBulkDelete = true,
        SupportsBulkMerge = true,
        SupportsStoredProcedures = true,
        SupportsFunctions = true
    };

    // Thread-safe caches to ensure string generation happens exactly ONCE per entity shape
    private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> InsertSqlCache = new();
    private static readonly ConcurrentDictionary<RuntimeTypeHandle, string> UpdateSqlCache = new();

    public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString);

    public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id)
        => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });

    public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code)
        => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.CodeColumn} = {Dialect.Parameter("Code")}", new { Code = code });

    public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids)
        => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} IN @Ids", new { Ids = ids });

    public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity)
    {
        var sql = InsertSqlCache.GetOrAdd(
            e.EntityType.TypeHandle,
            static (_, state) =>
            {
                var metadata = state.Metadata;
                var dialect = state.Dialect;

                var columns = new List<string>(metadata.Properties.Count);
                var values = new List<string>(metadata.Properties.Count);

                for (var i = 0; i < metadata.Properties.Count; i++)
                {
                    var p = metadata.Properties[i];

                    if (p.IsComputed || p.IsKey)
                        continue;

                    columns.Add(p.ColumnName);
                    values.Add(dialect.Parameter(p.PropertyName));
                }

                return $"INSERT INTO {metadata.TableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";
            },
            (Metadata: e, Dialect));

        return ForgeCommand.Text(sql, entity);
    }

    public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity)
    {
        var sql = UpdateSqlCache.GetOrAdd(
            e.EntityType.TypeHandle,
            static (_, state) =>
            {
                var metadata = state.Metadata;
                var dialect = state.Dialect;

                var sets = new List<string>(metadata.Properties.Count);

                for (var i = 0; i < metadata.Properties.Count; i++)
                {
                    var p = metadata.Properties[i];

                    if (p.IsComputed || p.IsKey)
                        continue;

                    sets.Add(p.ColumnName + " = " + dialect.Parameter(p.PropertyName));
                }

                if (sets.Count == 0)
                    throw new InvalidOperationException($"Type '{metadata.EntityType.Name}' has no updateable columns.");

                return $"UPDATE {metadata.TableName} SET {string.Join(", ", sets)} WHERE {metadata.KeyColumn} = {dialect.Parameter(metadata.KeyColumn)}";
            },
            (Metadata: e, Dialect));

        return ForgeCommand.Text(sql, entity);
    }
    public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id)
        => ForgeCommand.Text($"DELETE FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });

    public ForgeCommand BuildPage(ForgePageRequest r)
        => ForgeCommand.Text($"""SELECT * FROM ({r.Sql}) ForgePage ORDER BY {r.OrderBy} LIMIT {r.PageSize} OFFSET {r.Skip}""", r.Parameters);

    public ForgeCommand BuildCount(string baseSql, object? parameters = null)
        => ForgeCommand.Text($"SELECT COUNT(1) FROM ({baseSql}) ForgeCount", parameters);

    public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids)
        => ForgeCommand.Text($"DELETE FROM {tableName} WHERE {keyColumn} IN @Ids", new { Ids = ids });

    public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null)
        => ForgeCommand.Text($"SELECT {functionName}()", parameters);

    public ValueTask<int> BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default)
        => MySqlNativeBulk.BulkInsertAsync(connection, tableName, rows, cancellationToken);

    public ValueTask<int> BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
    {       
        return MySqlNativeBulk.BulkUpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);
    }

    public ValueTask<int> BulkMergeAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
    {        
        return MySqlNativeBulk.BulkUpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);
    }
}

internal static class BulkFallback
{
    private static readonly ConcurrentDictionary<(Type Type, string Table, string Key), string> UpdateSqlStatementCache = new();

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="connection"></param>
    /// <param name="tableName"></param>
    /// <param name="keys"></param>
    /// <param name="keyColumn"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async ValueTask<int> DeleteAsync<TKey>(
    DbConnection connection,
    string tableName,
    IReadOnlyCollection<TKey> keys,
    string keyColumn,
    CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return 0;
       await MySqlNativeBulk.BulkDeleteAsync(
                    connection,
                    tableName,
                    keys,
                    keyColumn,
                    cancellationToken);
        return 1;

    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="connection"></param>
    /// <param name="tableName"></param>
    /// <param name="rows"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static ValueTask InsertAsync<T>(
       DbConnection connection,
       string tableName,
       IReadOnlyCollection<T> rows,
       CancellationToken cancellationToken = default)
    {
        if (rows is null || rows.Count == 0)
            return ValueTask.CompletedTask;

        var props = ForgeProviderAdo.PropertyCache<T>.Properties;

        if (props.Length == 0)
            return ValueTask.CompletedTask;

        var columns = new string[props.Length];
        var parameters = new string[props.Length];

        for (var i = 0; i < props.Length; i++)
        {
            columns[i] = props[i].Info.Name;
            parameters[i] = props[i].ParamName;
        }

        var sql =
            $"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})";

        var task = ForgeProviderAdo.ExecuteManyAsync(
            connection,
            sql,
            rows,
            cancellationToken);

        return task.IsCompletedSuccessfully
            ? ValueTask.CompletedTask
            : Awaited(task);

        static async ValueTask Awaited(ValueTask<int> task)
        {
            await task.ConfigureAwait(false);
        }
    }
    public static ValueTask UpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken ct)
    {
        var sql = UpdateSqlStatementCache.GetOrAdd((typeof(T), tableName, keyColumn), static key =>
        {
            var (type, table, pk) = key;
            var props = PropertyCache<T>.Properties
                .Where(p => !p.Info.Name.Equals(pk, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Info.Name)
                .ToList();

            if (props.Count == 0)
                throw new InvalidOperationException($"Type {type.Name} has no valid properties to update.");

            var set = string.Join(", ", props.Select(p => $"{p} = @{p}"));
            return $"UPDATE {table} SET {set} WHERE {pk} = @{pk}";
        });

        // Discard the task's integer result to return a clean, non-allocating ValueTask wrapper
        var task = ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
        return task.IsCompletedSuccessfully ? ValueTask.CompletedTask : ConvertToValueTask(task);
    }

    private static async ValueTask ConvertToValueTask(ValueTask<int> task) => await task.ConfigureAwait(false);
}

internal static class ForgeProviderAdo
{
    // Reusable static generic metadata cache handled by the runtime engine per variations of T
    internal static class PropertyCache<T>
    {
        public static readonly (PropertyInfo Info, string ParamName, Type DeclaredType)[] Properties =
            typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsScalar(p.PropertyType))
                .Select(p => (p, "@" + p.Name, p.PropertyType))
                .ToArray();
    }

    public static ValueTask<int> ExecuteManyAsync<T>(DbConnection connection, string sql, IReadOnlyCollection<T> rows, CancellationToken cancellationToken)
    {
        if (rows is null || rows.Count == 0)
            return new ValueTask<int>(0);

        var cachedProps = PropertyCache<T>.Properties;
        if (cachedProps.Length == 0)
            return new ValueTask<int>(0);

        return ExecuteManyInternalAsync(connection, sql, rows, cachedProps, cancellationToken);
    }

    private static async ValueTask<int> ExecuteManyInternalAsync<T>(
        DbConnection connection,
        string sql,
        IReadOnlyCollection<T> rows,
        (PropertyInfo Info, string ParamName, Type DeclaredType)[] cachedProps,
        CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var dbParameters = new DbParameter[cachedProps.Length];
        for (int i = 0; i < cachedProps.Length; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = cachedProps[i].ParamName;
            command.Parameters.Add(parameter);
            dbParameters[i] = parameter;
        }

        try { command.Prepare(); } catch { /* Resilient execution fallback */ }

        var total = 0;
        foreach (var row in rows)
        {
            for (int i = 0; i < cachedProps.Length; i++)
            {
                ref readonly var propMetadata = ref cachedProps[i];
                var rawValue = ForgeProviderAccessors.Get(propMetadata.Info, row!);
                dbParameters[i].Value = NormalizeValue(rawValue, propMetadata.DeclaredType) ?? DBNull.Value;
            }

            total += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsScalar(Type type)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? NormalizeValue(object? value, Type declaredType)
    {
        if (value is null) return null;

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

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