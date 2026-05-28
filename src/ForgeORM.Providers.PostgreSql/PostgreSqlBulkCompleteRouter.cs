using ForgeORM.Core;
using ForgeORM.Core.Bulk;
using Npgsql;

namespace ForgeORM.Providers.PostgreSql;

/// <summary>
/// PostgreSQL bulk equivalent:
/// InsertBulk -> COPY.
/// UpdateBulk -> temp table + UPDATE FROM.
/// DeleteBulk -> temp table + DELETE USING.
/// GraphUpdate -> temp table + UPDATE FROM / MERGE-like flow.
/// </summary>
internal static class PostgreSqlBulkCompleteRouter
{
    public static async ValueTask<int> InsertBulkAsync<T>(
        NpgsqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        options ??= ForgeProviderBulkOptionsDefaults.Current;

        return options.PostgreSqlStrategy == ForgeBulkOperationStrategy.PostgreSqlTempTable
            ? await InsertViaTempTableAsync(connection, plan, rows, cancellationToken).ConfigureAwait(false)
            : await PostgreSqlNativeBulk.BulkInsertAsync(connection, plan.TableName, rows, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> UpdateBulkAsync<T>(
        NpgsqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;
        await PostgreSqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        await PostgreSqlNativeBulk.BulkUpdateAsync(connection, plan.TableName, rows, keyColumn, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    public static async ValueTask<int> DeleteBulkAsync<TKey>(
        NpgsqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0) return 0;
        await PostgreSqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        await PostgreSqlNativeBulk.BulkDeleteAsync(connection, plan.TableName, keys, keyColumn, cancellationToken).ConfigureAwait(false);
        return keys.Count;  
    }

    public static ValueTask<int> GraphUpdateAsync<T>(
        NpgsqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
        => UpdateBulkAsync(connection, plan, rows, keyColumn, options, cancellationToken);

    private static async ValueTask<int> InsertViaTempTableAsync<T>(
        NpgsqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken)
    {
        await PostgreSqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        return await PostgreSqlNativeBulk.BulkInsertAsync(connection, plan.TableName, rows, cancellationToken).ConfigureAwait(false);
    }
}
