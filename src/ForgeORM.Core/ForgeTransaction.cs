using System.Data.Common;
using ForgeORM.Core.Performance;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed  class ForgeTransaction : IForgeTransaction
{
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;

    private ForgeTransaction(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection; _transaction = transaction;
    }

    /// <summary>
    /// Executes the Begin operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <returns>The result of the Begin operation.</returns>
    public static ForgeTransaction Begin(DbConnection connection) => new(connection, connection.BeginTransaction());
    /// <summary>
    /// Executes the BeginAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="ct">The ct value.</param>
    /// <returns>The result of the BeginAsync operation.</returns>
    public static async ValueTask<ForgeTransaction> BeginAsync(DbConnection connection, CancellationToken ct) => new(connection, await connection.BeginTransactionAsync(ct));

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.Query<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds).ToList();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgePerformancePipeline.QueryAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the Execute operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Execute operation.</returns>
    public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.Execute(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds);

    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public ValueTask<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgeAdo.ExecuteAsync(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
        => ForgeAdo.ExecuteScalar<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgePerformancePipeline.ExecuteScalarAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    /// <summary>
    /// Executes the Commit operation.
    /// </summary>
    public void Commit() => _transaction.Commit();
    /// <summary>
    /// Executes the CommitAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CommitAsync operation.</returns>
    //public ValueTask CommitAsync(CancellationToken cancellationToken = default)
    //=> new(_transaction.CommitAsync(cancellationToken));
    /// <summary>
    /// Executes the Rollback operation.
    /// </summary>
    public void Rollback() => _transaction.Rollback();
    /// <summary>
    /// Executes the RollbackAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RollbackAsync operation.</returns>
    //public ValueTask RollbackAsync(CancellationToken cancellationToken = default) => new(_transaction.RollbackAsync(cancellationToken));
    /// <summary>
    /// Executes the Dispose operation.
    /// </summary>
    /// <param name="_connection">The _connection value.</param>
    public void Dispose() { _transaction.Dispose(); _connection.Dispose(); }
    /// <summary>
    /// Executes the DisposeAsync operation.
    /// </summary>
    /// <param name="_connection">The _connection value.</param>
    /// <returns>The result of the DisposeAsync operation.</returns>
    public async ValueTask DisposeAsync() { await _transaction.DisposeAsync(); await _connection.DisposeAsync(); }
}
