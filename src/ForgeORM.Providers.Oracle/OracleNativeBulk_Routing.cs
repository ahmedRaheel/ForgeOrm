using Oracle.ManagedDataAccess.Client;

namespace ForgeORM.Providers.Oracle;

/// <summary>
/// Oracle bulk routing:
/// Insert/Update/Delete use array binding and MERGE/delete strategies.
/// No SQL Server-specific APIs are used.
/// </summary>
internal static class OracleNativeBulkRouting
{
    public static async ValueTask<int> InsertAsync<T>(
        OracleConnection connection,
        OracleBulkPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        await OracleBulkEnsure.EnsureArrayBindingReadyAsync(connection, plan, cancellationToken)
            .ConfigureAwait(false);

        return await OracleNativeBulk.BulkInsertAsync(connection, plan.TableName, rows, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<int> UpdateAsync<T>(
        OracleConnection connection,
        OracleBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        await OracleBulkEnsure.EnsureArrayBindingReadyAsync(connection, plan, cancellationToken)
            .ConfigureAwait(false);

        return await OracleNativeBulk.BulkUpdateAsync(connection, plan.TableName, rows, keyColumn, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<int> DeleteAsync<TKey>(
        OracleConnection connection,
        OracleBulkPlan plan,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        await OracleBulkEnsure.EnsureArrayBindingReadyAsync(connection, plan, cancellationToken)
            .ConfigureAwait(false);

        return await OracleNativeBulk.BulkDeleteAsync(connection, plan.TableName, keys, keyColumn, cancellationToken)
            .ConfigureAwait(false);
    }
}
