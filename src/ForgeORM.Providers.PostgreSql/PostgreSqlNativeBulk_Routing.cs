using Npgsql;

namespace ForgeORM.Providers.PostgreSql;

/// <summary>
/// PostgreSQL bulk routing:
/// Insert: COPY into table or temp staging table.
/// Update: temp staging table + UPDATE ... FROM.
/// Delete: temp staging table + DELETE ... USING.
/// No SQL Server-specific APIs are used.
/// </summary>
internal static class PostgreSqlNativeBulkRouting
{
    public static async ValueTask<int> InsertAsync<T>(
        NpgsqlConnection connection,
        PostgreSqlBulkPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        await PostgreSqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken)
            .ConfigureAwait(false);

        return await PostgreSqlNativeBulk.BulkInsertAsync(connection, plan.TableName, rows, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<int> UpdateAsync<T>(
        NpgsqlConnection connection,
        PostgreSqlBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        await PostgreSqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken)
            .ConfigureAwait(false);

        return await PostgreSqlNativeBulk.BulkUpdateAsync(connection, plan.TableName, rows, keyColumn, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<int> DeleteAsync<TKey>(
        NpgsqlConnection connection,
        PostgreSqlBulkPlan plan,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        await PostgreSqlBulkEnsure.EnsureTempTableAsync(connection, plan, cancellationToken)
            .ConfigureAwait(false);

        return await PostgreSqlNativeBulk.BulkDeleteAsync(connection, plan.TableName, keys, keyColumn, cancellationToken)
            .ConfigureAwait(false);
    }
}
