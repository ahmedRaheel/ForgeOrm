using System.Data;
using System.Data.Common;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb : IForgeDb
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

    public IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.Query<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds).ToList();
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.QueryAsync<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    public T QueryFirst<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(sql, parameters, timeoutSeconds).First();

    public async Task<T> QueryFirstAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryAsync<T>(sql, parameters, timeoutSeconds, cancellationToken)).First();

    public T? QueryFirstOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(sql, parameters, timeoutSeconds).FirstOrDefault();

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryAsync<T>(sql, parameters, timeoutSeconds, cancellationToken)).FirstOrDefault();

    public T QuerySingle<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(sql, parameters, timeoutSeconds).Single();

    public async Task<T> QuerySingleAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryAsync<T>(sql, parameters, timeoutSeconds, cancellationToken)).Single();

    public T? QuerySingleOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(sql, parameters, timeoutSeconds).SingleOrDefault();

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryAsync<T>(sql, parameters, timeoutSeconds, cancellationToken)).SingleOrDefault();

    public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.Execute(c, sql, parameters, timeoutSeconds: timeoutSeconds);
    }

    public async Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.ExecuteAsync(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    public T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.ExecuteScalar<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.ExecuteScalarAsync<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    public IForgeGridReader QueryMultiple(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        var c = CreateConnection();
        c.Open();
        var command = ForgeAdo.CreateCommand(c, sql, parameters, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(c, command, command.ExecuteReader());
    }

    public async Task<IForgeGridReader> QueryMultipleAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        var command = ForgeAdo.CreateCommand(c, sql, parameters, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(c, command, await command.ExecuteReaderAsync(cancellationToken));
    }

    public IEnumerable<T> QueryProcedure<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.Query<T>(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds).ToList();
    }

    public async Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.QueryAsync<T>(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    public T? QueryProcedureSingleOrDefault<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
        => QueryProcedure<T>(procedureName, parameters, timeoutSeconds).SingleOrDefault();

    public async Task<T?> QueryProcedureSingleOrDefaultAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryProcedureAsync<T>(procedureName, parameters, timeoutSeconds, cancellationToken)).SingleOrDefault();

    public int ExecuteProcedure(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.Execute(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds);
    }

    public async Task<int> ExecuteProcedureAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.ExecuteAsync(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    public T? ExecuteProcedureScalar<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.ExecuteScalar<T>(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds);
    }

    public async Task<T?> ExecuteProcedureScalarAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.ExecuteScalarAsync<T>(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    public IForgeGridReader QueryProcedureMultiple(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        var c = CreateConnection();
        c.Open();
        var command = ForgeAdo.CreateCommand(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(c, command, command.ExecuteReader());
    }

    public async Task<IForgeGridReader> QueryProcedureMultipleAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        var command = ForgeAdo.CreateCommand(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(c, command, await command.ExecuteReaderAsync(cancellationToken));
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
    public async Task<IReadOnlyList<IDictionary<string, object?>>> QueryDynamicAsync(
    string sql,
    object? parameters = null,
    CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();

        return await ForgeAdo.QueryDynamicAsync(
            connection,
            sql,
            parameters,
            cancellationToken: cancellationToken);
    }
}
