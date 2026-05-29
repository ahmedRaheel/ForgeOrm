using ForgeORM.Abstractions;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ForgeORM.Providers.Oracle;

public sealed class OracleForgeProvider : IForgeDatabaseProvider
{
    public string ProviderName => "Oracle";
    public ForgeSqlDialect Dialect { get; } = new() { Name = "Oracle", ParameterPrefix = ":", OpenIdentifier = "\"", CloseIdentifier = "\"" };
    public ForgeProviderCapabilities Capabilities { get; } = new()
    {
        SupportsBulkInsert = true,
        SupportsBulkUpdate = true,
        SupportsBulkDelete = true,
        SupportsBulkMerge = true,
        SupportsStoredProcedures = true,
        SupportsFunctions = true,
        SupportsArrayParameters = true,
        SupportsRefCursor = true
    };

    /// <summary>
    /// Executes the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    public DbConnection CreateConnection(string connectionString) => new OracleConnection(connectionString);

    /// <summary>
    /// Executes the BuildGetById operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="id">The id value.</param>
    /// <returns>The result of the BuildGetById operation.</returns>
    public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id) => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });
    /// <summary>
    /// Executes the BuildGetByCode operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="code">The code value.</param>
    /// <returns>The result of the BuildGetByCode operation.</returns>
    public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code) => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.CodeColumn} = {Dialect.Parameter("Code")}", new { Code = code });
    /// <summary>
    /// Executes the BuildGetByIds operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="pairs">The pairs value.</param>
    /// <param name="pairs">The pairs value.</param>
    /// <param name="x">The x value.</param>
    /// <returns>The result of the BuildGetByIds operation.</returns>
    public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids) { var pairs = ids.Select((id, i) => new { id, name = $"Id{i}" }).ToList(); return ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} IN ({string.Join(", ", pairs.Select(x => ":" + x.name))})", pairs.ToDictionary(x => x.name, x => (object)x.id)); }
    /// <summary>
    /// Executes the BuildInsert operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the BuildInsert operation.</returns>
    public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildInsertSql(e), entity);
    /// <summary>
    /// Executes the BuildUpdate operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the BuildUpdate operation.</returns>
    public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildUpdateSql(e), entity);
    /// <summary>
    /// Executes the BuildDelete operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="id">The id value.</param>
    /// <returns>The result of the BuildDelete operation.</returns>
    public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id) => ForgeCommand.Text($"DELETE FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });
    /// <summary>
    /// Executes the BuildPage operation.
    /// </summary>
    /// <param name="r">The r value.</param>
    /// <returns>The result of the BuildPage operation.</returns>
    public ForgeCommand BuildPage(ForgePageRequest r) => ForgeCommand.Text($"""SELECT * FROM ({r.Sql}) ForgePage ORDER BY {r.OrderBy} OFFSET {r.Skip} ROWS FETCH NEXT {r.PageSize} ROWS ONLY""", r.Parameters);
    /// <summary>
    /// Executes the BuildCount operation.
    /// </summary>
    /// <param name="baseSql">The baseSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the BuildCount operation.</returns>
    public ForgeCommand BuildCount(string baseSql, object? parameters = null) => ForgeCommand.Text($"SELECT COUNT(1) FROM ({baseSql}) ForgeCount", parameters);
    /// <summary>
    /// Executes the BuildBulkDelete operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="ids">The ids value.</param>
    /// <returns>The result of the BuildBulkDelete operation.</returns>
    public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids) => ForgeCommand.Text($"DELETE FROM {tableName} WHERE {keyColumn} IN @Ids", new { Ids = ids });
    /// <summary>
    /// Executes the BuildFunctionScalar operation.
    /// </summary>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the BuildFunctionScalar operation.</returns>
    public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null) => ForgeCommand.Text($"SELECT {functionName}() FROM DUAL", parameters);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, ForgeProviderBulkOptions? bulkOptions = null, CancellationToken cancellationToken = default) => OracleNativeBulk.BulkInsertAsync(connection, tableName, rows, bulkOptions ?? ForgeProviderBulkOptionsDefaults.Current, cancellationToken);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, ForgeProviderBulkOptions? bulkOptions = null, CancellationToken cancellationToken = default) => OracleNativeBulk.BulkUpdateAsync(connection, tableName, rows, keyColumn, bulkOptions ?? ForgeProviderBulkOptionsDefaults.Current, cancellationToken);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask BulkMergeAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default) => BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);

    private string BuildInsertSql(ForgeEntityMetadata e)
    {
        var props = e.Properties.Where(p => !p.IsComputed && !p.IsKey).ToList();
        var columns = string.Join(", ", props.Select(p => p.ColumnName));
        var values = string.Join(", ", props.Select(p => Dialect.Parameter(p.PropertyName)));
        return $"INSERT INTO {e.TableName} ({columns}) VALUES ({values})";
    }

    private string BuildUpdateSql(ForgeEntityMetadata e)
    {
        var props = e.Properties.Where(p => !p.IsComputed && !p.IsKey).ToList();
        var sets = string.Join(", ", props.Select(p => p.ColumnName + " = " + Dialect.Parameter(p.PropertyName)));
        return $"UPDATE {e.TableName} SET {sets} WHERE {e.KeyColumn} = {Dialect.Parameter(e.KeyColumn)}";
    }
}

internal static class BulkFallback
{
    private static readonly ConcurrentDictionary<(Type Type, string Table, string Key), string> UpdateSqlCache = new();

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
        await
                 OracleNativeBulk.BulkDeleteAsync(
                     connection,
                     tableName,
                     keys,
                     keyColumn,
                     cancellationToken);
        return 1;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async ValueTask InsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken ct)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && IsScalar(p.PropertyType)).ToList();
        var columns = string.Join(", ", props.Select(p => p.Name));
        var values = string.Join(", ", props.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
        await ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ValueTask UpdateAsync<T>(
        DbConnection connection,
        string tableName,
        IReadOnlyCollection<T> rows,
        string keyColumn,
        CancellationToken ct)
    {
        // 1. Guard clause to avoid processing overhead or state machine instantiation
        if (rows == null || rows.Count == 0)
            return ValueTask.CompletedTask;

        // 2. Retrieve or compile the SQL update string. This executes exactly ONCE per unique schema setup.
        var sql = UpdateSqlCache.GetOrAdd((typeof(T), tableName, keyColumn), static key =>
        {
            var (type, table, pk) = key;

            // Extract all updateable properties (excluding the primary key column)
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsScalar(p.PropertyType) && !p.Name.Equals(pk, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name)
                .ToList();

            if (props.Count == 0)
                throw new InvalidOperationException($"Type {type.Name} has no valid scalar properties to update.");

            // Build the SET clause: Field1 = @Field1, Field2 = @Field2
            var setClause = string.Join(", ", props.Select(p => $"{p} = @{p}"));

            // Build the final optimized SQL statement
            return $"UPDATE {table} SET {setClause} WHERE {pk} = @{pk}";
        });

        // 3. Perfect-forward the ValueTask straight down to the optimized batch executor.
        // This elides the async state machine wrapper allocation entirely.
        return ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
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

}


internal static class ForgeProviderAdo
{
    // High-performance static generic cache initialized once per type T by the CLR.
    // This reduces structural type lookup overhead to an absolute zero runtime allocation cost.
    private static class PropertyCache<T>
    {
        public static readonly (PropertyInfo Info, string ParamName, Type DeclaredType)[] Properties =
            typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && IsScalar(p.PropertyType))
                .Select(p => (p, "@" + p.Name, p.PropertyType))
                .ToArray();
    }

    /// <summary>
    /// Executes the T operation using a zero-allocation parameter mutation architecture.
    /// </summary>
    public static async ValueTask ExecuteManyAsync<T>(
    DbConnection connection,
    string sql,
    IReadOnlyCollection<T> rows,
    CancellationToken cancellationToken)
    {
        // 1. Guard clauses on the synchronous hot path.
        // If there is nothing to process, we exit with zero allocation using a pre-cached token.
        if (rows is null || rows.Count == 0)
            return;

        var cachedProps = PropertyCache<T>.Properties;
        if (cachedProps.Length == 0)
            return;

        // 2. Delegate to the internal async loop execution path
        await ExecuteManyInternalAsync(connection, sql, rows, cachedProps, cancellationToken).ConfigureAwait(false);
    }

    // Keeping the async state machine generation safely separated here
    private static async ValueTask ExecuteManyInternalAsync<T>(
        DbConnection connection,
        string sql,
        IReadOnlyCollection<T> rows,
        (System.Reflection.PropertyInfo Info, string ParamName, Type DeclaredType)[] cachedProps,
        CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Allocate EXACTLY ONE command and a single set of reusable parameters for the entire batch
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var dbParameters = new DbParameter[cachedProps.Length];
        for (int i = 0; i < cachedProps.Length; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = cachedProps[i].ParamName;
            command.Parameters.Add(parameter);
            dbParameters[i] = parameter; // Direct array access optimization
        }

        // Attempt command preparation if supported by the underlying ADO.NET provider
        try { command.Prepare(); } catch { /* Fallback for engines lacking explicit preparation support */ }

        // Allocation-Free Execution Loop
        foreach (var row in rows)
        {
            for (int i = 0; i < cachedProps.Length; i++)
            {
                ref readonly var propMetadata = ref cachedProps[i];
                var rawValue = ForgeProviderAccessors.Get(propMetadata.Info, row!);

                // Mutate the parameter values in-place on the heap pool
                dbParameters[i].Value = NormalizeValue(rawValue, propMetadata.DeclaredType) ?? DBNull.Value;
            }

            // Execute the database write without returning or capturing an unused integer result
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? NormalizeValue(object? value, Type declaredType)
    {
        if (value is null)
            return null;

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual == typeof(DateTime))
        {
            var dateTime = (DateTime)value;
            return dateTime == default || dateTime < new DateTime(1753, 1, 1)
                ? DateTime.UtcNow
                : dateTime;
        }

        if (actual == typeof(DateTimeOffset))
        {
            var dto = (DateTimeOffset)value;
            return dto == default ? DateTimeOffset.UtcNow : dto;
        }

        return value;
    }
}
