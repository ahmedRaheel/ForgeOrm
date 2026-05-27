using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;
internal static class ForgeProviderBulkFallback
{
    public static ValueTask<int> InsertRowsAsync<T>(
        DbConnection connection,
        ForgeBulkEntityPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        if (connection is SqlConnection sqlConnection)
        {
            return ForgeSqlServerTvpFallback.InsertAsync(
                sqlConnection,
                plan,
                rows,
                cancellationToken);
        }

        throw new NotSupportedException(
            $"Bulk insert fallback is not implemented for provider '{connection.GetType().Name}'. Use provider-native bulk provider.");
    }

    public static ValueTask<int> UpdateRowsAsync<T>(
        DbConnection connection,
        ForgeBulkEntityPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return ValueTask.FromResult(0);

        if (connection is SqlConnection sqlConnection)
        {
            return ForgeSqlServerTvpFallback.UpdateAsync(
                sqlConnection,
                plan,
                rows,
                cancellationToken);
        }

        throw new NotSupportedException(
            $"Bulk update fallback is not implemented for provider '{connection.GetType().Name}'. Use provider-native bulk provider.");
    }

    public static ValueTask<int> DeleteRowsAsync<TKey>(
        DbConnection connection,
        ForgeBulkEntityPlan plan,
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return ValueTask.FromResult(0);

        if (connection is SqlConnection sqlConnection)
        {
            return ForgeSqlServerTvpFallback.DeleteAsync(
                sqlConnection,
                plan,
                keys,
                cancellationToken);
        }

        throw new NotSupportedException(
            $"Bulk delete fallback is not implemented for provider '{connection.GetType().Name}'. Use provider-native bulk provider.");
    }
}
internal static class ForgeSqlServerTvpFallback
{
    public static async ValueTask<int> InsertAsync<T>(
        SqlConnection connection,
        ForgeBulkEntityPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        var table = CreateTable(plan, rows);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.InsertSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = table;

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> UpdateAsync<T>(
        SqlConnection connection,
        ForgeBulkEntityPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        var table = CreateTable(plan, rows);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.UpdateSql; // MERGE

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = table;

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<int> DeleteAsync<TKey>(
        SqlConnection connection,
        ForgeBulkEntityPlan plan,
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken = default)
    {
        var table = new DataTable();

        var keyType = Nullable.GetUnderlyingType(typeof(TKey)) ?? typeof(TKey);
        table.Columns.Add(plan.KeyColumn, keyType);

        for (var i = 0; i < keys.Count; i++)
        {
            var row = table.NewRow();
            row[0] = keys[i] is null ? DBNull.Value : keys[i]!;
            table.Rows.Add(row);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = plan; // DELETE JOIN

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.KeyTvpTypeName;
        parameter.Value = table;

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DataTable CreateTable<T>(
        ForgeBulkEntityPlan plan,
        IReadOnlyList<T> rows)
    {
        var table = new DataTable();

        for (var i = 0; i < plan.Columns.Count; i++)
        {
            var column = plan.Columns[i];
            var clrType = Nullable.GetUnderlyingType(column.DeclaringType) ?? column.DeclaringType;

            if (clrType.IsEnum)
                clrType = typeof(string);

            table.Columns.Add(column.Name, clrType);
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var dataRow = table.NewRow();

            for (var c = 0; c < plan.Columns.Count; c++)
            {
                var column = plan.Columns[c];
                var value = column.GetValue(rows[r]);

                dataRow[c] = value switch
                {
                    null => DBNull.Value,
                    Enum e => e.ToString(),
                    _ => value
                };
            }

            table.Rows.Add(dataRow);
        }

        return table;
    }
}