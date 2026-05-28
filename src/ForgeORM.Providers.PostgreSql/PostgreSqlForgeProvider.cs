using ForgeORM.Abstractions;
using Npgsql;
using System.Data.Common;
using System.Reflection;

namespace ForgeORM.Providers.PostgreSql;

public sealed class PostgreSqlForgeProvider : IForgeDatabaseProvider
{
    public string ProviderName => "PostgreSql";
    public ForgeSqlDialect Dialect { get; } = new() { Name = "PostgreSql", ParameterPrefix = "@", OpenIdentifier = "\"", CloseIdentifier = "\"" };
    public ForgeProviderCapabilities Capabilities { get; } = new()
    {
        SupportsBulkInsert = true,
        SupportsBulkUpdate = true,
        SupportsBulkDelete = true,
        SupportsBulkMerge = true,
        SupportsStoredProcedures = true,
        SupportsFunctions = true,
        SupportsArrayParameters = true,
        SupportsCopy = true,
        SupportsReturningClause = true,
        SupportsJsonColumns = true
    };

    /// <summary>
    /// Executes the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString);

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
    /// <returns>The result of the BuildGetByIds operation.</returns>
    public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids) { return ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} = ANY(@Ids)", new { Ids = ids.ToArray() }); }
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
    public ForgeCommand BuildPage(ForgePageRequest r) => ForgeCommand.Text($"""SELECT * FROM ({r.Sql}) ForgePage ORDER BY {r.OrderBy} LIMIT {r.PageSize} OFFSET {r.Skip}""", r.Parameters);
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
    public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null) => ForgeCommand.Text($"SELECT {functionName}()", parameters);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<int> BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default) => PostgreSqlNativeBulk.BulkInsertAsync(connection, tableName, rows, cancellationToken);
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
    public ValueTask<int> BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default) => PostgreSqlNativeBulk.BulkUpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);
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
    public async ValueTask BulkMergeAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default)
    {
        _ = await PostgreSqlNativeBulk.BulkUpdateAsync(connection, tableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
    }

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

       await  PostgreSqlNativeBulk.BulkDeleteAsync(
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
    public static async ValueTask InsertAsync<T>(
    DbConnection connection,
    string tableName,
    IReadOnlyCollection<T> rows,
    CancellationToken ct)
    {
        if (rows is null || rows.Count == 0)
            return;

        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalar(p.PropertyType))
            .ToList();

        var columns = string.Join(", ", props.Select(p => p.Name));
        var values = string.Join(", ", props.Select(p => "@" + p.Name));

        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

        await ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct)
            .ConfigureAwait(false);
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
    public static async ValueTask UpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken ct)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && IsScalar(p.PropertyType) && !p.Name.Equals(keyColumn, StringComparison.OrdinalIgnoreCase)).ToList();
        var set = string.Join(", ", props.Select(p => p.Name + " = @" + p.Name));
        var sql = $"UPDATE {tableName} SET {set} WHERE {keyColumn} = @{keyColumn}";
       await ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
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
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async ValueTask<int> ExecuteManyAsync<T>(DbConnection connection, string sql, IReadOnlyCollection<T> rows, CancellationToken cancellationToken)
    {
        var total = 0;
        foreach (var row in rows)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && IsScalar(p.PropertyType)))
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@" + prop.Name;
                parameter.Value = NormalizeValue(ForgeProviderAccessors.Get(prop, row!), prop.PropertyType) ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
            total += await command.ExecuteNonQueryAsync(cancellationToken);
        }
        return total;
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
