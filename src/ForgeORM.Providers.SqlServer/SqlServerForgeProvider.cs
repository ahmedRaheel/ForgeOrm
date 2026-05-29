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
        SupportsTableValuedParameters = true,
        SupportsJsonColumns = true,
        SupportsJsonBulkOperations = true,
        SupportsTemporalTables = true,
        SupportsVectorSearch = false
    };

    /// <summary>
    /// Executes the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString);

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
    /// <param name="Ids">The Ids value.</param>
    /// <returns>The result of the BuildGetByIds operation.</returns>
    public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids) { return ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} IN @Ids", new { Ids = ids }); }
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
    public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null) => ForgeCommand.Text($"SELECT dbo.{functionName}()", parameters);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, ForgeProviderBulkOptions? bulkOptions = null, CancellationToken cancellationToken = default) => SqlServerNativeBulk.BulkInsertAsync(connection, tableName, rows, bulkOptions ?? ForgeProviderBulkOptionsDefaults.Current, cancellationToken);
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
    public async ValueTask BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, ForgeProviderBulkOptions? bulkOptions = null, CancellationToken cancellationToken = default)
    {
        _ = await SqlServerNativeBulk.BulkUpdateAsync(connection, tableName, rows as IReadOnlyList<T> ?? rows.ToArray(), keyColumn, bulkOptions ?? ForgeProviderBulkOptionsDefaults.Current, cancellationToken).ConfigureAwait(false);
    }
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
        _ = await SqlServerNativeBulk.BulkUpdateAsync(connection, tableName, rows as IReadOnlyList<T> ?? rows.ToArray(), keyColumn, ForgeProviderBulkOptionsDefaults.Current, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Executes a provider-native bulk delete operation.</summary>
    public async ValueTask BulkDeleteAsync<TKey>(DbConnection connection, string tableName, IReadOnlyCollection<TKey> keys, string keyColumn, ForgeProviderBulkOptions? bulkOptions = null, CancellationToken cancellationToken = default)
    {
        _ = await SqlServerNativeBulk.BulkDeleteAsync(connection, tableName, keys as IReadOnlyList<TKey> ?? keys.ToArray(), keyColumn, bulkOptions ?? ForgeProviderBulkOptionsDefaults.Current, cancellationToken).ConfigureAwait(false);
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
