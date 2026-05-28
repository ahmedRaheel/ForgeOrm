using ForgeORM.Core;
using ForgeORM.Core.Bulk;
using MySqlConnector;

namespace ForgeORM.Providers.MySql;

/// <summary>
/// MySQL bulk equivalent:
/// InsertBulk -> multi-row insert.
/// UpdateBulk -> temp table + UPDATE JOIN.
/// DeleteBulk -> temp table + DELETE JOIN.
/// GraphUpdate -> temp table + UPDATE JOIN.
/// </summary>
internal static class MySqlBulkCompleteRouter
{
    public static ValueTask<int> InsertBulkAsync<T>(
        MySqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
        => rows.Count == 0
            ? ValueTask.FromResult(0)
            : MySqlNativeBulk.BulkInsertAsync(connection, plan.TableName, rows, cancellationToken);

    public static async ValueTask<int> UpdateBulkAsync<T>(
        MySqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        await MySqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        await MySqlNativeBulk.BulkUpdateAsync(connection, plan.TableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public static async ValueTask<int> DeleteBulkAsync<TKey>(
        MySqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0) return 0;
        await MySqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        return await MySqlNativeBulk.BulkDeleteAsync(connection, plan.TableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
    }

    public static ValueTask<int> GraphUpdateAsync<T>(
        MySqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
        => UpdateBulkAsync(connection, plan, rows, keyColumn, options, cancellationToken);
}
