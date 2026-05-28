using ForgeORM.Core;
using ForgeORM.Core.Bulk;
using Oracle.ManagedDataAccess.Client;

namespace ForgeORM.Providers.Oracle;

/// <summary>
/// Oracle bulk equivalent:
/// InsertBulk -> Array Binding.
/// UpdateBulk -> Array Binding / MERGE.
/// DeleteBulk -> Array Binding delete.
/// GraphUpdate -> MERGE.
/// </summary>
internal static class OracleBulkCompleteRouter
{
    public static async ValueTask<int> InsertBulkAsync<T>(
        OracleConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        await OracleBulkEnsure.EnsureArrayBindingReadyAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        return await OracleNativeBulk.BulkInsertAsync(connection, plan.TableName, rows, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> UpdateBulkAsync<T>(
        OracleConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        await OracleBulkEnsure.EnsureArrayBindingReadyAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        return await OracleNativeBulk.BulkUpdateAsync(connection, plan.TableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> DeleteBulkAsync<TKey>(
        OracleConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0) return 0;
        await OracleBulkEnsure.EnsureArrayBindingReadyAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        return await OracleNativeBulk.BulkDeleteAsync(connection, plan.TableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
    }

    public static ValueTask<int> GraphUpdateAsync<T>(
        OracleConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
        => UpdateBulkAsync(connection, plan, rows, keyColumn, options, cancellationToken);
}
