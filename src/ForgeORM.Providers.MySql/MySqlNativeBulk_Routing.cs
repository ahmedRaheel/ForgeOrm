using ForgeORM.Core;
using MySqlConnector;

namespace ForgeORM.Providers.MySql;

/// <summary>
/// MySQL bulk routing:
/// Insert: batched multi-row insert.
/// Update: temp staging table + UPDATE JOIN.
/// Delete: temp staging table + DELETE JOIN.
/// No SQL Server-specific APIs are used.
/// </summary>
internal static class MySqlNativeBulkRouting
{
    public static async ValueTask<int> InsertAsync<T>(
        MySqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        await MySqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken)
            .ConfigureAwait(false);

        return await MySqlNativeBulk.BulkInsertAsync(connection, plan.TableName, rows, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<int> UpdateAsync<T>(
        MySqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        await MySqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken)
            .ConfigureAwait(false);

        await MySqlNativeBulk.BulkUpdateAsync(connection, plan.TableName, rows, keyColumn, cancellationToken)
            .ConfigureAwait(false);
        return rows.Count;
    }

    public static async ValueTask<int> DeleteAsync<TKey>(
        MySqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        await MySqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken)
            .ConfigureAwait(false);

        return await MySqlNativeBulk.BulkDeleteAsync(connection, plan.TableName, keys, keyColumn, cancellationToken)
            .ConfigureAwait(false);
    }
}
