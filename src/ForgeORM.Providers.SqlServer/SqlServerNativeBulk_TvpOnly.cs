using System.Collections;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;

namespace ForgeORM.Providers.SqlServer;

/// <summary>
/// SQL Server TVP-only bulk implementation.
/// Primary path: SqlDataRecord TVP.
/// Fallback path: DataTable TVP.
/// This implementation intentionally does not use SqlBulkCopy.
/// </summary>
internal static class SqlServerNativeBulkTvpOnly
{
    public static async ValueTask<int> InsertAsync<T>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return 0;

        await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(
            connection,
            plan,
            SqlServerTableTypePurpose.InsertOrUpdate,
            cancellationToken).ConfigureAwait(false);

        try
        {
            return await InsertWithSqlDataRecordTvpAsync(
                connection,
                plan,
                rows,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (SqlServerBulkFallbackPolicy.CanFallback(ex))
        {
            return await InsertWithDataTableFallbackAsync(
                connection,
                plan,
                rows,
                cancellationToken).ConfigureAwait(false);
        }
    }

    public static async ValueTask<int> UpdateAsync<T>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
            return 0;

        await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(
            connection,
            plan,
            SqlServerTableTypePurpose.InsertOrUpdate,
            cancellationToken).ConfigureAwait(false);

        try
        {
            return await UpdateWithSqlDataRecordTvpAsync(
                connection,
                plan,
                rows,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (SqlServerBulkFallbackPolicy.CanFallback(ex))
        {
            return await UpdateWithDataTableFallbackAsync(
                connection,
                plan,
                rows,
                cancellationToken).ConfigureAwait(false);
        }
    }

    public static async ValueTask<int> DeleteAsync<TKey>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0)
            return 0;

        await SqlServerBulkEnsure.EnsureTableTypeCompatibleAsync(
            connection,
            plan,
            SqlServerTableTypePurpose.DeleteKeyOnly,
            cancellationToken).ConfigureAwait(false);

        try
        {
            return await DeleteWithSqlDataRecordTvpAsync(
                connection,
                plan,
                keys,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (SqlServerBulkFallbackPolicy.CanFallback(ex))
        {
            return await DeleteWithDataTableFallbackAsync(
                connection,
                plan,
                keys,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<int> InsertWithSqlDataRecordTvpAsync<T>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
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

    private static async ValueTask<int> InsertWithDataTableFallbackAsync<T>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
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

    private static async ValueTask<int> UpdateWithSqlDataRecordTvpAsync<T>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
        IReadOnlyList<T> rows,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = plan.UpdateSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.TvpTypeName;
        parameter.Value = new SqlDataRecordRows<T>(rows, plan);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> UpdateWithDataTableFallbackAsync<T>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
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

    private static async ValueTask<int> DeleteWithSqlDataRecordTvpAsync<TKey>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = plan.DeleteSql;

        var parameter = command.Parameters.Add("@Rows", SqlDbType.Structured);
        parameter.TypeName = plan.KeyTvpTypeName;
        parameter.Value = new SqlDataRecordKeyRows<TKey>(keys, plan);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask<int> DeleteWithDataTableFallbackAsync<TKey>(
        SqlConnection connection,
        SqlServerInsertPlan plan,
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

    private sealed class SqlDataRecordRows<T> : IEnumerable<SqlDataRecord>
    {
        private readonly IReadOnlyList<T> _rows;
        private readonly SqlServerInsertPlan _plan;

        public SqlDataRecordRows(IReadOnlyList<T> rows, SqlServerInsertPlan plan)
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
        private readonly SqlServerInsertPlan _plan;

        public SqlDataRecordKeyRows(IReadOnlyList<TKey> keys, SqlServerInsertPlan plan)
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

        if (actual.IsEnum)
        {
            record.SetString(ordinal, value.ToString()!);
            return;
        }

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
