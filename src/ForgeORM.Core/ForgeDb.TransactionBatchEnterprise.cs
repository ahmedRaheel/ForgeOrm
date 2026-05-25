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
    public async ValueTask TransactionAsync(Func<ForgeDbTransactionScope, ValueTask> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
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
    public async ValueTask<TResult> TransactionAsync<TResult>(Func<ForgeDbTransactionScope, ValueTask<TResult>> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
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
