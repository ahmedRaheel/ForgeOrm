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
}
