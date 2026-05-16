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

    /// <summary>
    /// Initializes or executes the ForgeDb operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <param name="provider">The provider value.</param>
    /// <param name="metadata">The metadata value.</param>
    /// <param name="analyzer">The analyzer value.</param>
    public ForgeDb(string connectionString, IForgeDatabaseProvider provider, IForgeEntityMetadataResolver metadata, IForgeQueryAnalyzer analyzer)
    {
        _connectionString = connectionString;
        Provider = provider;
        _metadata = metadata;
        _analyzer = analyzer;
    }

    private DbConnection CreateConnection() => Provider.CreateConnection(_connectionString);

    /// <summary>
    /// Initializes or executes the Query operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.Query<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds).ToList();
    }

    /// <summary>
    /// Initializes or executes the QueryAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.QueryAsync<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the QueryFirst operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public T QueryFirst<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(sql, parameters, timeoutSeconds).First();

    /// <summary>
    /// Initializes or executes the QueryFirstAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<T> QueryFirstAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryAsync<T>(sql, parameters, timeoutSeconds, cancellationToken)).First();

    /// <summary>
    /// Initializes or executes the QueryFirstOrDefault operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public T? QueryFirstOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(sql, parameters, timeoutSeconds).FirstOrDefault();

    /// <summary>
    /// Initializes or executes the QueryFirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryAsync<T>(sql, parameters, timeoutSeconds, cancellationToken)).FirstOrDefault();

    /// <summary>
    /// Initializes or executes the QuerySingle operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public T QuerySingle<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(sql, parameters, timeoutSeconds).Single();

    /// <summary>
    /// Initializes or executes the QuerySingleAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<T> QuerySingleAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryAsync<T>(sql, parameters, timeoutSeconds, cancellationToken)).Single();

    /// <summary>
    /// Initializes or executes the QuerySingleOrDefault operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public T? QuerySingleOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(sql, parameters, timeoutSeconds).SingleOrDefault();

    /// <summary>
    /// Initializes or executes the QuerySingleOrDefaultAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryAsync<T>(sql, parameters, timeoutSeconds, cancellationToken)).SingleOrDefault();

    /// <summary>
    /// Initializes or executes the Execute operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.Execute(c, sql, parameters, timeoutSeconds: timeoutSeconds);
    }

    /// <summary>
    /// Initializes or executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.ExecuteAsync(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the ExecuteScalar operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.ExecuteScalar<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds);
    }

    /// <summary>
    /// Initializes or executes the ExecuteScalarAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.ExecuteScalarAsync<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the QueryMultiple operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public IForgeGridReader QueryMultiple(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        var c = CreateConnection();
        c.Open();
        var command = ForgeAdo.CreateCommand(c, sql, parameters, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(c, command, command.ExecuteReader());
    }

    /// <summary>
    /// Initializes or executes the QueryMultipleAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<IForgeGridReader> QueryMultipleAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        var command = ForgeAdo.CreateCommand(c, sql, parameters, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(c, command, await command.ExecuteReaderAsync(cancellationToken));
    }

    /// <summary>
    /// Initializes or executes the QueryProcedure operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public IEnumerable<T> QueryProcedure<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.Query<T>(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds).ToList();
    }

    /// <summary>
    /// Initializes or executes the QueryProcedureAsync operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.QueryAsync<T>(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the QueryProcedureSingleOrDefault operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public T? QueryProcedureSingleOrDefault<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
        => QueryProcedure<T>(procedureName, parameters, timeoutSeconds).SingleOrDefault();

    /// <summary>
    /// Initializes or executes the QueryProcedureSingleOrDefaultAsync operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<T?> QueryProcedureSingleOrDefaultAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => (await QueryProcedureAsync<T>(procedureName, parameters, timeoutSeconds, cancellationToken)).SingleOrDefault();

    /// <summary>
    /// Initializes or executes the ExecuteProcedure operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public int ExecuteProcedure(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.Execute(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds);
    }

    /// <summary>
    /// Initializes or executes the ExecuteProcedureAsync operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<int> ExecuteProcedureAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.ExecuteAsync(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the ExecuteProcedureScalar operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public T? ExecuteProcedureScalar<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        using var c = CreateConnection();
        c.Open();
        return ForgeAdo.ExecuteScalar<T>(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds);
    }

    /// <summary>
    /// Initializes or executes the ExecuteProcedureScalarAsync operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<T?> ExecuteProcedureScalarAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        return await ForgeAdo.ExecuteScalarAsync<T>(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the QueryProcedureMultiple operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public IForgeGridReader QueryProcedureMultiple(string procedureName, object? parameters = null, int? timeoutSeconds = null)
    {
        var c = CreateConnection();
        c.Open();
        var command = ForgeAdo.CreateCommand(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(c, command, command.ExecuteReader());
    }

    /// <summary>
    /// Initializes or executes the QueryProcedureMultipleAsync operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<IForgeGridReader> QueryProcedureMultipleAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var c = CreateConnection();
        await c.OpenAsync(cancellationToken);
        var command = ForgeAdo.CreateCommand(c, procedureName, parameters, commandType: CommandType.StoredProcedure, timeoutSeconds: timeoutSeconds);
        return new ForgeGridReader(c, command, await command.ExecuteReaderAsync(cancellationToken));
    }

    /// <summary>
    /// Initializes or executes the ExecuteFunction operation.
    /// </summary>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public T? ExecuteFunction<T>(string functionName, object? parameters = null, int? timeoutSeconds = null)
    {
        var cmd = Provider.BuildFunctionScalar(functionName, parameters);
        return ExecuteScalar<T>(cmd.CommandText, cmd.Parameters, timeoutSeconds);
    }

    /// <summary>
    /// Initializes or executes the ExecuteFunctionAsync operation.
    /// </summary>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<T?> ExecuteFunctionAsync<T>(string functionName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var cmd = Provider.BuildFunctionScalar(functionName, parameters);
        return ExecuteScalarAsync<T>(cmd.CommandText, cmd.Parameters, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the QueryFunction operation.
    /// </summary>
    /// <param name="functionSql">The functionSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The operation result.</returns>
    public IReadOnlyList<T> QueryFunction<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(functionSql, parameters, timeoutSeconds).ToList();

    /// <summary>
    /// Initializes or executes the QueryFunctionAsync operation.
    /// </summary>
    /// <param name="functionSql">The functionSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<IReadOnlyList<T>> QueryFunctionAsync<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => QueryAsync<T>(functionSql, parameters, timeoutSeconds, cancellationToken);
    /// <summary>
    /// Initializes or executes the QueryDynamicAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
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
