using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

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

    public async ValueTask<int> ExecuteAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
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
