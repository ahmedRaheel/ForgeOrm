using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

public sealed class SqlServerForgeProvider : IForgeDatabaseProvider
{
    public string ProviderName => "SqlServer";
    public ForgeSqlDialect Dialect { get; } = new() { Name = "SqlServer", ParameterPrefix = "@", OpenIdentifier = "[", CloseIdentifier = "]" };
    public ForgeProviderCapabilities Capabilities { get; } = new()
    {
        SupportsBulkInsert = true,
        SupportsBulkUpdate = true,
        SupportsBulkDelete = true,
        SupportsBulkMerge = true,
        SupportsStoredProcedures = true,
        SupportsFunctions = true,
        SupportsTableValuedParameters = true
    };

    /// <summary>
    /// Initializes or executes the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The operation result.</returns>
    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

    /// <summary>
    /// Initializes or executes the BuildGetById operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="id">The id value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id) => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });
    /// <summary>
    /// Initializes or executes the BuildGetByCode operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="code">The code value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code) => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.CodeColumn} = {Dialect.Parameter("Code")}", new { Code = code });
    /// <summary>
    /// Initializes or executes the BuildGetByIds operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids) { return ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} IN @Ids", new { Ids = ids }); }
    /// <summary>
    /// Initializes or executes the BuildInsert operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="entity">The entity value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildInsertSql(e), entity);
    /// <summary>
    /// Initializes or executes the BuildUpdate operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="entity">The entity value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildUpdateSql(e), entity);
    /// <summary>
    /// Initializes or executes the BuildDelete operation.
    /// </summary>
    /// <param name="e">The e value.</param>
    /// <param name="id">The id value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id) => ForgeCommand.Text($"DELETE FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });
    /// <summary>
    /// Initializes or executes the BuildPage operation.
    /// </summary>
    /// <param name="r">The r value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildPage(ForgePageRequest r) => ForgeCommand.Text($"""SELECT * FROM ({r.Sql}) ForgePage ORDER BY {r.OrderBy} OFFSET {r.Skip} ROWS FETCH NEXT {r.PageSize} ROWS ONLY""", r.Parameters);
    /// <summary>
    /// Initializes or executes the BuildCount operation.
    /// </summary>
    /// <param name="baseSql">The baseSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildCount(string baseSql, object? parameters = null) => ForgeCommand.Text($"SELECT COUNT(1) FROM ({baseSql}) ForgeCount", parameters);
    /// <summary>
    /// Initializes or executes the BuildBulkDelete operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="ids">The ids value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids) => ForgeCommand.Text($"DELETE FROM {tableName} WHERE {keyColumn} IN @Ids", new { Ids = ids });
    /// <summary>
    /// Initializes or executes the BuildFunctionScalar operation.
    /// </summary>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null) => ForgeCommand.Text($"SELECT dbo.{functionName}()", parameters);

    /// <summary>
    /// Initializes or executes the BulkInsertAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default) => BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken);
    /// <summary>
    /// Initializes or executes the BulkUpdateAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default) => BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);
    /// <summary>
    /// Initializes or executes the BulkMergeAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task BulkMergeAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default) => BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);

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
    /// Initializes or executes the InsertAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The operation result.</returns>
    public static Task InsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken ct)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
        var columns = string.Join(", ", props.Select(p => p.Name));
        var values = string.Join(", ", props.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
        return ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
    }

    /// <summary>
    /// Initializes or executes the UpdateAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The operation result.</returns>
    public static Task UpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken ct)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead && !p.Name.Equals(keyColumn, StringComparison.OrdinalIgnoreCase)).ToList();
        var set = string.Join(", ", props.Select(p => p.Name + " = @" + p.Name));
        var sql = $"UPDATE {tableName} SET {set} WHERE {keyColumn} = @{keyColumn}";
        return ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
    }
}


internal static class ForgeProviderAdo
{
    /// <summary>
    /// Initializes or executes the ExecuteManyAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public static async Task<int> ExecuteManyAsync<T>(DbConnection connection, string sql, IReadOnlyCollection<T> rows, CancellationToken cancellationToken)
    {
        var total = 0;
        foreach (var row in rows)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead))
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@" + prop.Name;
                parameter.Value = prop.GetValue(row) ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
            total += await command.ExecuteNonQueryAsync(cancellationToken);
        }
        return total;
    }
}
