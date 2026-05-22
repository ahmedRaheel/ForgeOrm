using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

/// <summary>
/// Centralized enterprise orchestration APIs. These methods deliberately route every operation through
/// ForgePerformancePipeline so raw SQL, query builder, batch, transaction, scalar and graph paths use
/// the same compiler/execution framework.
/// </summary>
public partial class ForgeDb
{
    /// <summary>
    /// Executes multiple ForgeORM operations inside one connection and transaction.
    /// </summary>
    public async Task TransactionAsync(Func<ForgeDbTransactionScope, Task> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var connection = CreateConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        var scope = new ForgeDbTransactionScope(connection, transaction);
        try
        {
            await action(scope).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Executes multiple ForgeORM operations inside one connection and transaction and returns a result.
    /// </summary>
    public async Task<TResult> TransactionAsync<TResult>(Func<ForgeDbTransactionScope, Task<TResult>> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var connection = CreateConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        var scope = new ForgeDbTransactionScope(connection, transaction);
        try
        {
            var result = await action(scope).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Starts a command batch. Batches use the same compiled pipeline as normal commands.
    /// </summary>
    public ForgeBatchBuilder Batch() => new(CreateConnection());
}

/// <summary>
/// Transaction scope that shares one connection/transaction across every operation.
/// </summary>
public sealed class ForgeDbTransactionScope
{
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;

    internal ForgeDbTransactionScope(DbConnection connection, DbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public ValueTask<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgePerformancePipeline.QueryAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    public ValueTask<T?> FirstOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgePerformancePipeline.FirstOrDefaultAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    public ValueTask<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgePerformancePipeline.ExecuteAsync(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);

    public ValueTask<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
        => ForgePerformancePipeline.ExecuteScalarAsync<T>(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
}

public sealed class ForgeBatchBuilder : IAsyncDisposable, IDisposable
{
    private readonly DbConnection _connection;
    private readonly List<ForgeBatchCommand> _commands = new(8);
    private bool _disposed;

    internal ForgeBatchBuilder(DbConnection connection) => _connection = connection;

    public ForgeBatchBuilder Execute(string sql, object? parameters = null, CommandType commandType = CommandType.Text)
    {
        _commands.Add(new ForgeBatchCommand(sql, parameters, commandType));
        return this;
    }

    public ForgeBatchBuilder Insert(string sql, object? parameters = null) => Execute(sql, parameters);
    public ForgeBatchBuilder Update(string sql, object? parameters = null) => Execute(sql, parameters);
    public ForgeBatchBuilder Delete(string sql, object? parameters = null) => Execute(sql, parameters);

    public async Task<int> ExecuteAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var tx = await _connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        var affected = 0;
        try
        {
            for (var i = 0; i < _commands.Count; i++)
            {
                var command = _commands[i];
                affected += await ForgePerformancePipeline.ExecuteAsync(_connection, command.Sql, command.Parameters, tx, command.CommandType, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return affected;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}

internal readonly record struct ForgeBatchCommand(string Sql, object? Parameters, CommandType CommandType);
