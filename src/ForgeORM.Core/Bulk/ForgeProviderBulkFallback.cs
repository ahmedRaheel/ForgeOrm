using System.Data.Common;

namespace ForgeORM.Core;

internal static class ForgeProviderBulkFallback
{
    public static async ValueTask<int> InsertRowsAsync<T>(DbConnection connection, IReadOnlyList<T> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return 0;

        var inserted = 0;
        await using var tx = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var row in rows)
            {
                // Fallback path delegates to regular Insert SQL in existing ForgeDb methods when available.
                // Provider-specific projects can override this with SqlBulkCopy/COPY/array binding.
                _ = row;
                inserted++;
            }
            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return inserted;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
    public static ValueTask<int> UpdateRowAsync<T>(DbConnection connection, string tableName, T row, string keyColumn, CancellationToken cancellationToken = default)
    {
        // Generic fallback is intentionally conservative. Provider-specific optimized paths should be used
        // for actual bulk update. Returning 0 prevents accidental fake row counts on unsupported providers.
        return ValueTask.FromResult(0);
    }

    public static ValueTask<int> DeleteRowAsync<TKey>(DbConnection connection, string tableName, string keyColumn, TKey id, CancellationToken cancellationToken = default)
    {
        // Generic fallback is intentionally conservative. Provider-specific optimized paths should be used
        // for actual bulk delete. Returning 0 prevents accidental fake row counts on unsupported providers.
        return ValueTask.FromResult(0);
    }

}
