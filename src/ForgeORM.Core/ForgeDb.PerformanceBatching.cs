using System.Data.Common;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Provider-aware insert batching. SQL Server can be upgraded to SqlBulkCopy, PostgreSQL to COPY,
    /// MySQL to multi-row INSERT, and Oracle to array binding without changing this public API.
    /// </summary>
    public ValueTask<int> InsertManyAsync<T>(IEnumerable<T> rows, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return InsertBulkAsync(rows as IReadOnlyList<T> ?? rows.ToArray(), cancellationToken);
    }

    public ValueTask<int> UpdateManyAsync<T>(IEnumerable<T> rows, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return UpdateBulkAsync(rows as IReadOnlyList<T> ?? rows.ToArray(), cancellationToken: cancellationToken);
    }

    public ValueTask<int> DeleteManyAsync<T>(IEnumerable<object> ids, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var normalized = ids.ToArray();
        return DeleteBulkAsync<T, object>(normalized, cancellationToken);
    }

    /// <summary>
    /// Inserts many rows using the native provider bulk path.
    /// SQL Server uses TVP + INSERT SELECT, PostgreSQL uses COPY/temp staging, MySQL uses multi-row batching,
    /// and Oracle uses array binding/MERGE where available.
    /// </summary>
    public async ValueTask<int> InsertBulkAsync<T>(IReadOnlyList<T> rows, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
            return 0;

        var metadata = _metadata.Resolve<T>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await Provider.BulkInsertAsync(connection, metadata.TableName, rows, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    /// <summary>
    /// Updates many rows using the native provider bulk path.
    /// SQL Server uses TVP + MERGE, PostgreSQL uses temp table + UPDATE FROM,
    /// MySQL uses temp table + UPDATE JOIN, and Oracle uses array binding/MERGE.
    /// </summary>
    public async ValueTask<int> UpdateBulkAsync<T>(IReadOnlyList<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
            return 0;

        var metadata = _metadata.Resolve<T>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await Provider.BulkUpdateAsync(connection, metadata.TableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    /// <summary>
    /// Deletes many rows using the native provider bulk path when available.
    /// </summary>
    public async ValueTask<int> DeleteBulkAsync<T, TKey>(IReadOnlyList<TKey> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
            return 0;

        var metadata = _metadata.Resolve<T>();

        // Existing provider contract only exposes BuildBulkDelete, so this remains provider-neutral.
        // SQL Server providers can override this through provider-specific packages without changing the public API.
        var objectIds = ids.Select(static x => (object?)x).ToArray();
        var command = Provider.BuildBulkDelete(metadata.TableName, metadata.KeyColumn, objectIds.OfType<int>().ToArray());
        return await ExecuteAsync(command.CommandText, command.Parameters, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Graph bulk update entry point. Parent rows are routed through UpdateBulkAsync;
    /// child graph traversal stays in the graph service to preserve relationship semantics.
    /// </summary>
    public ValueTask<int> GraphUpdateAsync<T>(IReadOnlyList<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default)
        => UpdateBulkAsync(rows, keyColumn, cancellationToken);

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

    private ValueTask<int> InsertBatchInternalAsync<T>(IReadOnlyList<T> batch, CancellationToken cancellationToken)
        => InsertBulkAsync(batch, cancellationToken);
}
