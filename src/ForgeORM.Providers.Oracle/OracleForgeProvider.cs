using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;
using Oracle.ManagedDataAccess.Client;

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

    public DbConnection CreateConnection(string connectionString) => new OracleConnection(connectionString);

    public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id) => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });
    public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code) => ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.CodeColumn} = {Dialect.Parameter("Code")}", new { Code = code });
    public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids) { var pairs = ids.Select((id, i) => new { id, name = $"Id{i}" }).ToList(); return ForgeCommand.Text($"SELECT * FROM {e.TableName} WHERE {e.KeyColumn} IN ({string.Join(", ", pairs.Select(x => ":" + x.name))})", pairs.ToDictionary(x => x.name, x => (object)x.id)); }
    public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildInsertSql(e), entity);
    public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildUpdateSql(e), entity);
    public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id) => ForgeCommand.Text($"DELETE FROM {e.TableName} WHERE {e.KeyColumn} = {Dialect.Parameter("Id")}", new { Id = id });
    public ForgeCommand BuildPage(ForgePageRequest r) => ForgeCommand.Text($"""SELECT * FROM ({r.Sql}) ForgePage ORDER BY {r.OrderBy} OFFSET {r.Skip} ROWS FETCH NEXT {r.PageSize} ROWS ONLY""", r.Parameters);
    public ForgeCommand BuildCount(string baseSql, object? parameters = null) => ForgeCommand.Text($"SELECT COUNT(1) FROM ({baseSql}) ForgeCount", parameters);
    public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids) => ForgeCommand.Text($"DELETE FROM {tableName} WHERE {keyColumn} IN @Ids", new { Ids = ids });
    public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null) => ForgeCommand.Text($"SELECT {functionName}() FROM DUAL", parameters);

    public Task BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default) => BulkFallback.InsertAsync(connection, tableName, rows, cancellationToken);
    public Task BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default) => BulkFallback.UpdateAsync(connection, tableName, rows, keyColumn, cancellationToken);
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
    public static Task InsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken ct)
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead).ToList();
        var columns = string.Join(", ", props.Select(p => p.Name));
        var values = string.Join(", ", props.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
        return ForgeProviderAdo.ExecuteManyAsync(connection, sql, rows, ct);
    }

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
