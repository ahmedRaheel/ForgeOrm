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

            total += await InsertBulkAsync(batch, cancellationToken).ConfigureAwait(false);
            batch.Clear();
        }

        if (batch.Count > 0)
            total += await InsertBulkAsync(batch, cancellationToken).ConfigureAwait(false);

        return total;
    }

    public async ValueTask<int> UpdateManyAsync<T>(IEnumerable<T> rows, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var total = 0;
        foreach (var batch in rows.Chunk(Math.Max(1, batchSize)))
            total += await UpdateBulkAsync(batch, cancellationToken: cancellationToken).ConfigureAwait(false);
        return total;
    }

    public async ValueTask<int> DeleteManyAsync<T>(IEnumerable<object> ids, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var total = 0;
        foreach (var batch in ids.Chunk(Math.Max(1, batchSize)))
        {
            var intIds = batch.Select(static x => Convert.ToInt32(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            total += await DeleteBulkAsync<T>(intIds, cancellationToken).ConfigureAwait(false);
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
        return await InsertBulkAsync(batch.ToArray(), cancellationToken).ConfigureAwait(false);
    }
}
