using System.Data;
using System.Data.Common;
using Dapper;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed partial class ForgeDb : IForgeDb
{
    private readonly string _connectionString;
    private readonly IForgeEntityMetadataResolver _metadata;
    private readonly IForgeQueryAnalyzer _analyzer;

    public IForgeDatabaseProvider Provider { get; }

    public ForgeDb(string connectionString, IForgeDatabaseProvider provider, IForgeEntityMetadataResolver metadata, IForgeQueryAnalyzer analyzer)
    {
        _connectionString = connectionString;
        Provider = provider;
        _metadata = metadata;
        _analyzer = analyzer;
    }

    private DbConnection CreateConnection() => Provider.CreateConnection(_connectionString);

    private static CommandDefinition Command(ForgeCommand command, CancellationToken cancellationToken = default)
        => new(command.CommandText, command.Parameters, commandTimeout: command.TimeoutSeconds, commandType: command.CommandType, cancellationToken: cancellationToken);

    public IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.Query<T>(sql, parameters, commandTimeout: timeoutSeconds).ToList();
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        var rows = await c.QueryAsync<T>(new CommandDefinition(sql, parameters, commandTimeout: timeoutSeconds, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public T QueryFirst<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.QueryFirst<T>(sql, parameters, commandTimeout: timeoutSeconds);
    }

    public async Task<T> QueryFirstAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await c.QueryFirstAsync<T>(new CommandDefinition(sql, parameters, commandTimeout: timeoutSeconds, cancellationToken: cancellationToken));
    }

    public T? QueryFirstOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.QueryFirstOrDefault<T>(sql, parameters, commandTimeout: timeoutSeconds);
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await c.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, parameters, commandTimeout: timeoutSeconds, cancellationToken: cancellationToken));
    }

    public T QuerySingle<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.QuerySingle<T>(sql, parameters, commandTimeout: timeoutSeconds);
    }

    public async Task<T> QuerySingleAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await c.QuerySingleAsync<T>(new CommandDefinition(sql, parameters, commandTimeout: timeoutSeconds, cancellationToken: cancellationToken));
    }

    public T? QuerySingleOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.QuerySingleOrDefault<T>(sql, parameters, commandTimeout: timeoutSeconds);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await c.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, parameters, commandTimeout: timeoutSeconds, cancellationToken: cancellationToken));
    }

    public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.Execute(sql, parameters, commandTimeout: timeoutSeconds);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await c.ExecuteAsync(new CommandDefinition(sql, parameters, commandTimeout: timeoutSeconds, cancellationToken: cancellationToken));
    }

    public T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.ExecuteScalar<T>(sql, parameters, commandTimeout: timeoutSeconds);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await c.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, commandTimeout: timeoutSeconds, cancellationToken: cancellationToken));
    }

    public IForgeGridReader QueryMultiple(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        var c = CreateConnection();
        c.Open();
        return new ForgeGridReader(c, c.QueryMultiple(sql, parameters, commandTimeout: timeoutSeconds));
    }

    public async Task<IForgeGridReader> QueryMultipleAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        var grid = await c.QueryMultipleAsync(new CommandDefinition(sql, parameters, commandTimeout: timeoutSeconds, cancellationToken: cancellationToken));
        return new ForgeGridReader(c, grid);
    }

    public IEnumerable<T> QueryProcedure<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.Query<T>(procedureName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: timeoutSeconds).ToList();
    }

    public async Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        var rows = await c.QueryAsync<T>(new CommandDefinition(procedureName, parameters, commandTimeout: timeoutSeconds, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public T? QueryProcedureSingleOrDefault<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.QuerySingleOrDefault<T>(procedureName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: timeoutSeconds);
    }

    public async Task<T?> QueryProcedureSingleOrDefaultAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await c.QuerySingleOrDefaultAsync<T>(new CommandDefinition(procedureName, parameters, commandTimeout: timeoutSeconds, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }

    public int ExecuteProcedure(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.Execute(procedureName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: timeoutSeconds);
    }

    public async Task<int> ExecuteProcedureAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await c.ExecuteAsync(new CommandDefinition(procedureName, parameters, commandTimeout: timeoutSeconds, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }

    public T? ExecuteProcedureScalar<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return c.ExecuteScalar<T>(procedureName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: timeoutSeconds);
    }

    public async Task<T?> ExecuteProcedureScalarAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await c.ExecuteScalarAsync<T>(new CommandDefinition(procedureName, parameters, commandTimeout: timeoutSeconds, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
    }

    public IForgeGridReader QueryProcedureMultiple(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        var c = CreateConnection();
        c.Open();
        return new ForgeGridReader(c, c.QueryMultiple(procedureName, parameters, commandType: CommandType.StoredProcedure, commandTimeout: timeoutSeconds));
    }

    public async Task<IForgeGridReader> QueryProcedureMultipleAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        var grid = await c.QueryMultipleAsync(new CommandDefinition(procedureName, parameters, commandTimeout: timeoutSeconds, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken));
        return new ForgeGridReader(c, grid);
    }

    public T? ExecuteFunction<T>(string functionName, object? parameters = null, int? timeoutSeconds = null)
    {
        var cmd = Provider.BuildFunctionScalar(functionName, parameters);
        return ExecuteScalar<T>(cmd.CommandText, cmd.Parameters, timeoutSeconds);
    }

    public Task<T?> ExecuteFunctionAsync<T>(string functionName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var cmd = Provider.BuildFunctionScalar(functionName, parameters);
        return ExecuteScalarAsync<T>(cmd.CommandText, cmd.Parameters, timeoutSeconds, cancellationToken);
    }

    public IReadOnlyList<T> QueryFunction<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(functionSql, parameters, timeoutSeconds).ToList();

    public Task<IReadOnlyList<T>> QueryFunctionAsync<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => QueryAsync<T>(functionSql, parameters, timeoutSeconds, cancellationToken);
}
