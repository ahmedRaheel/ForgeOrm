using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

/// <summary>
/// Final SQL Server bulk routing:
/// 1. Try SqlDataRecord TVP first.
/// 2. If capability/setup failure occurs, fallback to DataTable TVP table-type parameter.
/// 3. Never use SqlBulkCopy.
/// </summary>
internal static class SqlServerNativeBulkNoSqlBulkCopyRouting
{
    public static async ValueTask<int> InsertAsync<T>(
        SqlConnection sqlConnection,
        SqlServerBulkPlan plan,
        IReadOnlyList<T> list,
        CancellationToken cancellationToken = default)
    {
        if (list.Count == 0)
            return 0;

        await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(
            sqlConnection,
            plan,
            SqlServerTableTypePurpose.InsertOrUpdate,
            cancellationToken).ConfigureAwait(false);

        try
        {
            return await SqlServerSqlDataRecordTvpBulk.InsertAsync(
                sqlConnection,
                plan,
                list,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (SqlServerBulkFallbackPolicy.CanFallback(ex))
        {
            return await SqlServerDataTableTvpBulk.InsertAsync(
                sqlConnection,
                plan,
                list,
                cancellationToken).ConfigureAwait(false);
        }
    }

    public static async ValueTask<int> UpdateAsync<T>(
        SqlConnection sqlConnection,
        SqlServerBulkPlan plan,
        IReadOnlyList<T> list,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (list.Count == 0)
            return 0;

        await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(
            sqlConnection,
            plan,
            SqlServerTableTypePurpose.InsertOrUpdate,
            cancellationToken).ConfigureAwait(false);

        try
        {
            return await SqlServerSqlDataRecordTvpBulk.UpdateAsync(
                sqlConnection,
                plan,
                list,
                keyColumn,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (SqlServerBulkFallbackPolicy.CanFallback(ex))
        {
            return await SqlServerDataTableTvpBulk.UpdateAsync(
                sqlConnection,
                plan,
                list,
                keyColumn,
                cancellationToken).ConfigureAwait(false);
        }
    }

    public static async ValueTask<int> DeleteAsync<TKey>(
        SqlConnection sqlConnection,
        SqlServerBulkPlan plan,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return 0;

        await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(
            sqlConnection,
            plan,
            SqlServerTableTypePurpose.DeleteKeyOnly,
            cancellationToken).ConfigureAwait(false);

        try
        {
            return await SqlServerSqlDataRecordTvpBulk.DeleteAsync(
                sqlConnection,
                plan,
                keys,
                keyColumn,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (SqlServerBulkFallbackPolicy.CanFallback(ex))
        {
            return await SqlServerDataTableTvpBulk.DeleteAsync(
                sqlConnection,
                plan,
                keys,
                keyColumn,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
