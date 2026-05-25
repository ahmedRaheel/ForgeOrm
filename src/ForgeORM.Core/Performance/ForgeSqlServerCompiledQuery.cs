using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Fully compiled SQL Server single-parameter query. This is the benchmark-grade path:
/// SqlConnection + SqlCommand + typed SqlParameter + SqlDataReader + Func&lt;SqlDataReader,T&gt;.
/// It caches SQL, parameter name, command behavior, typed parameter binder and materializer.
/// </summary>
public sealed class ForgeSqlServerCompiledQuery<T, TKey>
{
    private readonly string _connectionString;
    private readonly string _sql;
    private readonly string _parameterName;
    private readonly int? _timeoutSeconds;
    private readonly CommandBehavior _behavior;
    private readonly Action<SqlCommand, TKey> _bindParameter;
    private Func<SqlDataReader, T>? _materializer;

    internal ForgeSqlServerCompiledQuery(string connectionString, string sql, int? timeoutSeconds = null)
    {
        _connectionString = connectionString;
        _sql = sql;
        _timeoutSeconds = timeoutSeconds;
        _behavior = CommandBehavior.SingleRow | CommandBehavior.SequentialAccess;
        _parameterName = ForgeSqlServerParameterNameScanner.FirstOrDefault(sql) ?? "@Id";
        _bindParameter = ForgeSqlServerParameterShape<TKey>.CreateBinder(_parameterName);
    }

    /// <summary>Executes the compiled query synchronously without async state-machine allocation.</summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public T? Execute(TKey id)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = _sql;
        command.CommandType = CommandType.Text;
        if (_timeoutSeconds.HasValue)
            command.CommandTimeout = _timeoutSeconds.Value;

        _bindParameter(command, id);

        using var reader = command.ExecuteReader(_behavior);
        if (!reader.Read())
            return default;

        var materializer = _materializer;
        if (materializer is null)
        {
            materializer = ForgeSqlServerDirectMaterializerCache.GetOrCreate<T>(reader);
            Volatile.Write(ref _materializer, materializer);
        }

        return materializer(reader);
    }

    /// <summary>Executes the compiled query asynchronously using ValueTask.</summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public async ValueTask<T?> ExecuteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = _sql;
        command.CommandType = CommandType.Text;
        if (_timeoutSeconds.HasValue)
            command.CommandTimeout = _timeoutSeconds.Value;

        _bindParameter(command, id);

        await using var reader = await command.ExecuteReaderAsync(_behavior, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return default;

        var materializer = _materializer;
        if (materializer is null)
        {
            materializer = ForgeSqlServerDirectMaterializerCache.GetOrCreate<T>(reader);
            Volatile.Write(ref _materializer, materializer);
        }

        return materializer(reader);
    }
}

internal static class ForgeSqlServerParameterNameScanner
{
    public static string? FirstOrDefault(string sql)
    {
        if (string.IsNullOrEmpty(sql))
            return null;

        var span = sql.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] != '@')
                continue;
            if (i + 1 < span.Length && span[i + 1] == '@')
                continue;

            var start = i;
            i++;
            if (i >= span.Length || !(char.IsLetter(span[i]) || span[i] == '_'))
                continue;

            while (i < span.Length && (char.IsLetterOrDigit(span[i]) || span[i] == '_'))
                i++;

            return sql.Substring(start, i - start);
        }

        return null;
    }
}

internal static class ForgeSqlServerParameterShape<TKey>
{
    public static Action<SqlCommand, TKey> CreateBinder(string parameterName)
    {
        if (!parameterName.StartsWith('@'))
            parameterName = "@" + parameterName;

        var type = Nullable.GetUnderlyingType(typeof(TKey)) ?? typeof(TKey);

        if (type == typeof(int)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.Int, value);
        if (type == typeof(long)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.BigInt, value);
        if (type == typeof(short)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.SmallInt, value);
        if (type == typeof(byte)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.TinyInt, value);
        if (type == typeof(Guid)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.UniqueIdentifier, value);
        if (type == typeof(decimal)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.Decimal, value);
        if (type == typeof(double)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.Float, value);
        if (type == typeof(float)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.Real, value);
        if (type == typeof(bool)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.Bit, value);
        if (type == typeof(DateTime)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.DateTime2, value);
        if (type == typeof(DateTimeOffset)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.DateTimeOffset, value);
        if (type == typeof(DateOnly)) return (command, value) => AddAndSetDateOnly(command, parameterName, value);
        if (type == typeof(TimeOnly)) return (command, value) => AddAndSetTimeOnly(command, parameterName, value);
        if (type == typeof(TimeSpan)) return (command, value) => AddAndSet(command, parameterName, SqlDbType.Time, value);
        if (type == typeof(byte[])) return (command, value) => AddAndSet(command, parameterName, SqlDbType.VarBinary, value);
        if (type.IsEnum) return (command, value) => AddAndSetEnum(command, parameterName, value, type);

        return (command, value) => AddAndSet(command, parameterName, SqlDbType.NVarChar, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddAndSet(SqlCommand command, string parameterName, SqlDbType dbType, TKey value)
    {
        var p = command.Parameters.Add(parameterName, dbType);
        p.Value = value is null ? DBNull.Value : value;
    }

    private static void AddAndSetDateOnly(SqlCommand command, string parameterName, TKey value)
    {
        var p = command.Parameters.Add(parameterName, SqlDbType.Date);
        p.Value = value is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
    }

    private static void AddAndSetTimeOnly(SqlCommand command, string parameterName, TKey value)
    {
        var p = command.Parameters.Add(parameterName, SqlDbType.Time);
        p.Value = value is TimeOnly t ? t.ToTimeSpan() : DBNull.Value;
    }

    private static void AddAndSetEnum(SqlCommand command, string parameterName, TKey value, Type enumType)
    {
        var p = command.Parameters.Add(parameterName, ResolveEnumDbType(Enum.GetUnderlyingType(enumType)));
        p.Value = value is null ? DBNull.Value : Convert.ChangeType(value, Enum.GetUnderlyingType(enumType), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static SqlDbType ResolveEnumDbType(Type underlying)
    {
        if (underlying == typeof(byte)) return SqlDbType.TinyInt;
        if (underlying == typeof(short)) return SqlDbType.SmallInt;
        if (underlying == typeof(long)) return SqlDbType.BigInt;
        return SqlDbType.Int;
    }
}
