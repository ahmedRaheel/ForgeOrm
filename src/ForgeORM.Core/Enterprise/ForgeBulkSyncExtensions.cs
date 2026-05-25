using System.Linq.Expressions;
using ForgeORM.Core.Graph;
using System.Reflection;

namespace ForgeORM.Core;

public static class ForgeBulkSyncExtensions
{
    public static async ValueTask<ForgeSyncResult> SyncAsync<TEntity, TKey>(
        this ForgeDb db,
        IReadOnlyList<TEntity> rows,
        Expression<Func<TEntity, TKey>> key,
        Action<ForgeSyncOptions>? configure = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, new()
    {
        var options = new ForgeSyncOptions();
        configure?.Invoke(options);

        // Provider-specific high-performance implementation point:
        // SQL Server TVP/temp-table MERGE, PostgreSQL COPY + ON CONFLICT,
        // MySQL temp-table + ON DUPLICATE KEY, Oracle MERGE.
        var affected = 0;
        if (options.InsertMissing)
        {
            foreach (var row in rows)
            {
                affected += await db.InsertAsync(row, cancellationToken);
            }
        }

        return new ForgeSyncResult(affected, 0, 0, rows.Count);
    }
}
