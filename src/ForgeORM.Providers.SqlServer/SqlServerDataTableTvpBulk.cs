using System.Data;
using ForgeORM.Core;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Providers.SqlServer;

/// <summary>
/// SQL Server fallback/user-selected TVP implementation using DataTable as structured parameter.
/// This is not SqlBulkCopy. It still executes INSERT SELECT / MERGE / DELETE JOIN through @Rows.
/// </summary>
internal static class SqlServerDataTableTvpBulk
{
    public static async ValueTask<int> InsertAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return 0;

        var table = plan.CreateTable(rows);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.InsertSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = table;

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> UpdateAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return 0;

        var table = plan.CreateTable(rows);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.UpdateSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = table;

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> DeleteAsync<TKey>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<TKey> keys,
        string keyColumn,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return 0;

        var table = plan.CreateKeyTable(keys);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.DeleteSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.KeyTvpTypeName;
        parameter.Value = table;

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static ValueTask<int> GraphUpdateAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        string keyColumn,
        CancellationToken cancellationToken = default)
        => UpdateAsync(connection, plan, rows, keyColumn, cancellationToken);
}
