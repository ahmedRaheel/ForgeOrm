using System.Collections;
using System.Data;
using ForgeORM.Core;
using ForgeORM.Core.Bulk;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace ForgeORM.Providers.SqlServer;

/// <summary>
/// Complete SQL Server bulk router.
/// Default: SqlDataRecord.
/// Fallback: DataTable table type parameter.
/// Optional very-large insert: SqlBulkCopy.
/// </summary>
internal static class SqlServerBulkCompleteRouter
{
    public static async ValueTask<int> InsertBulkAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return 0;

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        if (options.AutoCreateProviderStructures)
        {
            await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(
                connection,
                plan,
                SqlServerTableTypePurpose.InsertOrUpdate,
                cancellationToken).ConfigureAwait(false);
        }

        if (options.SqlServerStrategy == ForgeBulkOperationStrategy.SqlBulkCopy ||
            rows.Count >= options.SqlBulkCopyThreshold)
        {
            return await InsertWithSqlBulkCopyAsync(connection, plan, rows, options, cancellationToken)
                .ConfigureAwait(false);
        }

        if (options.SqlServerStrategy == ForgeBulkOperationStrategy.TableTypeParameter)
        {
            return await InsertWithDataTableTvpAsync(connection, plan, rows, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            return await InsertWithSqlDataRecordTvpAsync(connection, plan, rows, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (SqlServerBulkFallbackPolicy.CanFallback(ex))
        {
            return await InsertWithDataTableTvpAsync(connection, plan, rows, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public static async ValueTask<int> UpdateBulkAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return 0;

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        if (options.AutoCreateProviderStructures)
        {
            await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(
                connection,
                plan,
                SqlServerTableTypePurpose.InsertOrUpdate,
                cancellationToken).ConfigureAwait(false);
        }

        if (options.SqlServerStrategy == ForgeBulkOperationStrategy.TableTypeParameter)
        {
            return await UpdateWithDataTableTvpAsync(connection, plan, rows, cancellationToken)
                .ConfigureAwait(false);
        }

        // SqlBulkCopy is insert-only; update always uses TVP + MERGE.
        try
        {
            return await UpdateWithSqlDataRecordTvpAsync(connection, plan, rows, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (SqlServerBulkFallbackPolicy.CanFallback(ex))
        {
            return await UpdateWithDataTableTvpAsync(connection, plan, rows, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public static async ValueTask<int> DeleteBulkAsync<TKey>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<TKey> keys,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return 0;

        options ??= ForgeProviderBulkOptionsDefaults.Current;

        if (options.AutoCreateProviderStructures)
        {
            await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(
                connection,
                plan,
                SqlServerTableTypePurpose.DeleteKeyOnly,
                cancellationToken).ConfigureAwait(false);
        }

        if (options.SqlServerStrategy == ForgeBulkOperationStrategy.TableTypeParameter)
        {
            return await DeleteWithDataTableTvpAsync(connection, plan, keys, cancellationToken)
                .ConfigureAwait(false);
        }

        // SqlBulkCopy is insert-only; delete always uses key TVP + DELETE JOIN.
        try
        {
            return await DeleteWithSqlDataRecordTvpAsync(connection, plan, keys, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (SqlServerBulkFallbackPolicy.CanFallback(ex))
        {
            return await DeleteWithDataTableTvpAsync(connection, plan, keys, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public static ValueTask<int> GraphUpdateAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions? options = null,
        CancellationToken cancellationToken = default)
        => UpdateBulkAsync(connection, plan, rows, options, cancellationToken);

    private static async ValueTask<int> InsertWithSqlDataRecordTvpAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = plan.InsertSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = new SqlDataRecordRows<T>(rows, plan);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> UpdateWithSqlDataRecordTvpAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = plan.UpdateSql; // MERGE

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = new SqlDataRecordRows<T>(rows, plan);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> DeleteWithSqlDataRecordTvpAsync<TKey>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = plan.DeleteSql; // DELETE JOIN

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.KeyTvpTypeName;
        parameter.Value = new SqlDataRecordKeyRows<TKey>(keys, plan);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> InsertWithDataTableTvpAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken)
    {
        var table = plan.CreateTable(rows);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.InsertSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = table;

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> UpdateWithDataTableTvpAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken)
    {
        var table = plan.CreateTable(rows);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.UpdateSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = table;

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> DeleteWithDataTableTvpAsync<TKey>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken)
    {
        var table = plan.CreateKeyTable(keys);

        await using var command = connection.CreateCommand();
        command.CommandText = plan.DeleteSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.KeyTvpTypeName;
        parameter.Value = table;

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> InsertWithSqlBulkCopyAsync<T>(
        SqlConnection connection,
        ForgeBulkPlan plan,
        IReadOnlyList<T> rows,
        ForgeProviderBulkOptions options,
        CancellationToken cancellationToken)
    {
        var table = plan.CreateTable(rows);

        using var bulk = new SqlBulkCopy(
            connection,
            SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.CheckConstraints,
            externalTransaction: null)
        {
            DestinationTableName = plan.QuotedTableName,
            BatchSize = Math.Min(Math.Max(rows.Count, 1), options.BatchSize),
            BulkCopyTimeout = options.CommandTimeoutSeconds,
            EnableStreaming = true
        };

        for (var i = 0; i < plan.ColumnNames.Length; i++)
            bulk.ColumnMappings.Add(plan.ColumnNames[i], plan.ColumnNames[i]);

        await bulk.WriteToServerAsync(table, cancellationToken).ConfigureAwait(false);
        return rows.Count;
    }

    private sealed class SqlDataRecordRows<T> : IEnumerable<SqlDataRecord>
    {
        private readonly IReadOnlyList<T> _rows;
        private readonly ForgeBulkPlan _plan;

        public SqlDataRecordRows(IReadOnlyList<T> rows, ForgeBulkPlan plan)
        {
            _rows = rows;
            _plan = plan;
        }

        public IEnumerator<SqlDataRecord> GetEnumerator()
        {
            for (var r = 0; r < _rows.Count; r++)
            {
                var record = new SqlDataRecord(_plan.SqlMetaData);
                var entity = _rows[r]!;

                for (var c = 0; c < _plan.ColumnNames.Length; c++)
                {
                    var value = NormalizeValue(_plan.GetValue(entity, c), _plan.GetDeclaredType(c));
                    SetRecordValue(record, c, value, _plan.GetDeclaredType(c));
                }

                yield return record;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class SqlDataRecordKeyRows<TKey> : IEnumerable<SqlDataRecord>
    {
        private readonly IReadOnlyList<TKey> _keys;
        private readonly ForgeBulkPlan _plan;

        public SqlDataRecordKeyRows(IReadOnlyList<TKey> keys, ForgeBulkPlan plan)
        {
            _keys = keys;
            _plan = plan;
        }

        public IEnumerator<SqlDataRecord> GetEnumerator()
        {
            for (var i = 0; i < _keys.Count; i++)
            {
                var record = new SqlDataRecord(_plan.KeySqlMetaData);
                var value = NormalizeValue(_keys[i], _plan.KeyDeclaredType);
                SetRecordValue(record, 0, value, _plan.KeyDeclaredType);
                yield return record;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static object? NormalizeValue(object? value, Type declaredType)
    {
        if (value is null)
            return null;

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual.IsEnum)
            return value.ToString();

        return value;
    }

    private static void SetRecordValue(SqlDataRecord record, int ordinal, object? value, Type declaredType)
    {
        if (value is null or DBNull)
        {
            record.SetDBNull(ordinal);
            return;
        }

        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;

        if (actual.IsEnum) { record.SetString(ordinal, value.ToString()!); return; }
        if (actual == typeof(int)) { record.SetInt32(ordinal, Convert.ToInt32(value)); return; }
        if (actual == typeof(long)) { record.SetInt64(ordinal, Convert.ToInt64(value)); return; }
        if (actual == typeof(short)) { record.SetInt16(ordinal, Convert.ToInt16(value)); return; }
        if (actual == typeof(byte)) { record.SetByte(ordinal, Convert.ToByte(value)); return; }
        if (actual == typeof(bool)) { record.SetBoolean(ordinal, Convert.ToBoolean(value)); return; }
        if (actual == typeof(string)) { record.SetString(ordinal, Convert.ToString(value)!); return; }
        if (actual == typeof(decimal)) { record.SetDecimal(ordinal, Convert.ToDecimal(value)); return; }
        if (actual == typeof(double)) { record.SetDouble(ordinal, Convert.ToDouble(value)); return; }
        if (actual == typeof(float)) { record.SetFloat(ordinal, Convert.ToSingle(value)); return; }
        if (actual == typeof(DateTime)) { record.SetDateTime(ordinal, Convert.ToDateTime(value)); return; }
        if (actual == typeof(Guid)) { record.SetGuid(ordinal, value is Guid g ? g : Guid.Parse(value.ToString()!)); return; }

        record.SetValue(ordinal, value);
    }
}
