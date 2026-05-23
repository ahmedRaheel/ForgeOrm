using System.Data;
using System.Data.Common;
using ForgeORM.Abstractions;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

public partial class ForgeDb : IForgeDb
{
    private readonly string _connectionString;
    private readonly IForgeEntityMetadataResolver _metadata;
    private readonly IForgeQueryAnalyzer _analyzer;

    /// <summary>
    /// Executes the ForgeDb operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <param name="provider">The provider value.</param>
    /// <param name="metadata">The metadata value.</param>
    /// <param name="analyzer">The analyzer value.</param>
    /// <returns>The result of the ForgeDb operation.</returns>
    public IForgeDatabaseProvider Provider { get; }

    /// <summary>
    /// Executes the ForgeDb operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <param name="provider">The provider value.</param>
    /// <param name="metadata">The metadata value.</param>
    /// <param name="analyzer">The analyzer value.</param>
    /// <returns>The result of the ForgeDb operation.</returns>
    public ForgeDb(string connectionString, IForgeDatabaseProvider provider, IForgeEntityMetadataResolver metadata, IForgeQueryAnalyzer analyzer)
    {
        _connectionString = connectionString;
        Provider = provider;
        _metadata = metadata;
        _analyzer = analyzer;
    }

    private DbConnection CreateConnection() => Provider.CreateConnection(_connectionString);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public IReadOnlyList<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.Query<T>(Provider, _connectionString, sql, parameters, timeoutSeconds);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.QueryAsync<T>(Provider, _connectionString, sql, parameters, timeoutSeconds, cancellationToken);

    /// <summary>Executes a SQL query when no parameters are needed and only a cancellation token is supplied.</summary>
    public Task<IReadOnlyList<T>> QueryAsync<T>(string sql, CancellationToken cancellationToken)
        => QueryAsync<T>(sql, parameters: null, timeoutSeconds: null, cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T QueryFirst<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        var item = QueryFirstOrDefault<T>(sql, parameters, timeoutSeconds);
        return item is not null ? item : throw new InvalidOperationException("Sequence contains no elements.");
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public async Task<T> QueryFirstAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var item = await QueryFirstOrDefaultAsync<T>(sql, parameters, timeoutSeconds, cancellationToken);
        return item is not null ? item : throw new InvalidOperationException("Sequence contains no elements.");
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? QueryFirstOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.FirstOrDefault<T>(Provider, _connectionString, sql, parameters, timeoutSeconds);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.FirstOrDefaultAsync<T>(Provider, _connectionString, sql, parameters, timeoutSeconds, cancellationToken);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T QuerySingle<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        var item = QuerySingleOrDefault<T>(sql, parameters, timeoutSeconds);
        return item is not null ? item : throw new InvalidOperationException("Sequence contains no elements.");
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public async Task<T> QuerySingleAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var item = await QuerySingleOrDefaultAsync<T>(sql, parameters, timeoutSeconds, cancellationToken);
        return item is not null ? item : throw new InvalidOperationException("Sequence contains no elements.");
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? QuerySingleOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.SingleOrDefault<T>(Provider, _connectionString, sql, parameters, timeoutSeconds);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.SingleOrDefaultAsync<T>(Provider, _connectionString, sql, parameters, timeoutSeconds, cancellationToken);

    /// <summary>
    /// Executes the Execute operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Execute operation.</returns>
    public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.Execute(Provider, _connectionString, sql, parameters, CommandType.Text, timeoutSeconds);

    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.ExecuteAsync(Provider, _connectionString, sql, parameters, CommandType.Text, timeoutSeconds, cancellationToken);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.Scalar<T>(Provider, _connectionString, sql, parameters, CommandType.Text, timeoutSeconds);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.ScalarAsync<T>(Provider, _connectionString, sql, parameters, CommandType.Text, timeoutSeconds, cancellationToken);

    /// <summary>
    /// Executes the QueryMultiple operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the QueryMultiple operation.</returns>
    public IForgeGridReader QueryMultiple(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.QueryMultiple(Provider, _connectionString, sql, parameters, timeoutSeconds);

    /// <summary>
    /// Executes the QueryMultipleAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the QueryMultipleAsync operation.</returns>
    public Task<IForgeGridReader> QueryMultipleAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.QueryMultipleAsync(Provider, _connectionString, sql, parameters, timeoutSeconds, cancellationToken);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public IEnumerable<T> QueryProcedure<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.Query<T>(Provider, _connectionString, procedureName, parameters, timeoutSeconds, CommandType.StoredProcedure);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.QueryAsync<T>(Provider, _connectionString, procedureName, parameters, timeoutSeconds, cancellationToken, CommandType.StoredProcedure);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? QueryProcedureSingleOrDefault<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.SingleOrDefault<T>(Provider, _connectionString, procedureName, parameters, timeoutSeconds, CommandType.StoredProcedure);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<T?> QueryProcedureSingleOrDefaultAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.SingleOrDefaultAsync<T>(Provider, _connectionString, procedureName, parameters, timeoutSeconds, cancellationToken, CommandType.StoredProcedure);

    /// <summary>
    /// Executes the ExecuteProcedure operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the ExecuteProcedure operation.</returns>
    public int ExecuteProcedure(string procedureName, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.Execute(Provider, _connectionString, procedureName, parameters, CommandType.StoredProcedure, timeoutSeconds);

    /// <summary>
    /// Executes the ExecuteProcedureAsync operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteProcedureAsync operation.</returns>
    public Task<int> ExecuteProcedureAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.ExecuteAsync(Provider, _connectionString, procedureName, parameters, CommandType.StoredProcedure, timeoutSeconds, cancellationToken);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? ExecuteProcedureScalar<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.Scalar<T>(Provider, _connectionString, procedureName, parameters, CommandType.StoredProcedure, timeoutSeconds);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<T?> ExecuteProcedureScalarAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.ScalarAsync<T>(Provider, _connectionString, procedureName, parameters, CommandType.StoredProcedure, timeoutSeconds, cancellationToken);

    /// <summary>
    /// Executes the QueryProcedureMultiple operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the QueryProcedureMultiple operation.</returns>
    public IForgeGridReader QueryProcedureMultiple(string procedureName, object? parameters = null, int? timeoutSeconds = null)
        => ForgeFrameworkExecutionPolicy.QueryMultiple(Provider, _connectionString, procedureName, parameters, timeoutSeconds, CommandType.StoredProcedure);

    /// <summary>
    /// Executes the QueryProcedureMultipleAsync operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the QueryProcedureMultipleAsync operation.</returns>
    public Task<IForgeGridReader> QueryProcedureMultipleAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeFrameworkExecutionPolicy.QueryMultipleAsync(Provider, _connectionString, procedureName, parameters, timeoutSeconds, cancellationToken, CommandType.StoredProcedure);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? ExecuteFunction<T>(string functionName, object? parameters = null, int? timeoutSeconds = null)
    {
        var cmd = Provider.BuildFunctionScalar(functionName, parameters);
        return ExecuteScalar<T>(cmd.CommandText, cmd.Parameters, timeoutSeconds);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<T?> ExecuteFunctionAsync<T>(string functionName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var cmd = Provider.BuildFunctionScalar(functionName, parameters);
        return ExecuteScalarAsync<T>(cmd.CommandText, cmd.Parameters, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="functionSql">The functionSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public IReadOnlyList<T> QueryFunction<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null)
        => Query<T>(functionSql, parameters, timeoutSeconds).ToList();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="functionSql">The functionSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<IReadOnlyList<T>> QueryFunctionAsync<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => QueryAsync<T>(functionSql, parameters, timeoutSeconds, cancellationToken);
    /// <summary>
    /// Executes the QueryDynamicAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the QueryDynamicAsync operation.</returns>
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

    Abstractions.ForgeQueryAnalysis IForgeDiagnostics.Analyze(string sql)
    {
        throw new NotImplementedException();
    }
}
