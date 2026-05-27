using System.Data.Common;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Provider-aware insert batching. SQL Server can be upgraded to SqlBulkCopy, PostgreSQL to COPY,
    /// MySQL to multi-row INSERT, and Oracle to array binding without changing this public API.
    /// </summary>
    public async ValueTask<int> InsertManyAsync<T>(IEnumerable<T> rows, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var total = 0;
        var batch = new List<T>(Math.Max(1, batchSize));

        foreach (var row in rows)
        {
            batch.Add(row);
            if (batch.Count < batchSize)
                continue;

            total += await InsertBatchInternalAsync(batch, cancellationToken).ConfigureAwait(false);
            batch.Clear();
        }

        if (batch.Count > 0)
            total += await InsertBatchInternalAsync(batch, cancellationToken).ConfigureAwait(false);

        return total;
    }

    public async ValueTask<int> UpdateManyAsync<T>(IEnumerable<T> rows, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var total = 0;
        foreach (var batch in rows.Chunk(Math.Max(1, batchSize)))
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var row in batch)
                    total += await UpdateAsync(row!, cancellationToken).ConfigureAwait(false);
                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        return total;
    }

    public async ValueTask<int> DeleteManyAsync<T>(IEnumerable<object> ids, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var total = 0;
        foreach (var batch in ids.Chunk(Math.Max(1, batchSize)))
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var id in batch)
                    total += await DeleteAsync<T>(id, cancellationToken).ConfigureAwait(false);
                await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        return total;
    }

    public async ValueTask<TResult> WithTransactionAsync<TResult>(Func<CancellationToken, ValueTask<TResult>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        await using var connection = CreateConnection();
        await using var context = await ForgeTransactionReuseContext.BeginAsync(connection, cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            await context.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch
        {
            await context.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public ValueTask<bool> WithTransactionAsync(Func<CancellationToken, ValueTask> operation, CancellationToken cancellationToken = default)
        =>  WithTransactionAsync(async ct => { await operation(ct).ConfigureAwait(false); return true; }, cancellationToken);

    private async ValueTask<int> InsertBatchInternalAsync<T>(IReadOnlyList<T> batch, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var strategy = ForgeProviderExecutionStrategySelector.Resolve(connection);
        return await strategy.ExecuteBulkInsertAsync(connection, batch, cancellationToken).ConfigureAwait(false);
    }
}
