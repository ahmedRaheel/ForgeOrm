using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ForgeORM.Core;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Dapper-style direct executor used by the framework-level pipeline for simple hot paths.
/// It is provider-neutral and applies to Query/First/Single/Scalar/Execute without introducing
/// a separate public framework. It intentionally avoids command plans, enum SQL rewrites,
/// dictionaries and LINQ when the SQL has a small scalar parameter shape.
/// </summary>
internal static class ForgeDirectQueryExecutor
{
    private static readonly ConcurrentDictionary<DirectParameterKey, DirectParameterAccessor> ParameterAccessors = new();
    private static readonly ConcurrentDictionary<DirectReaderKey, object> ReaderCache = new();

    public static bool CanUse(string sql, object? parameters, CommandType commandType)
    {
        if (ForgeEnterpriseRuntime.IsEnabled)
            return false;
        if (commandType != CommandType.Text || string.IsNullOrWhiteSpace(sql))
            return false;
        if (parameters is null)
            return true;
        if (parameters is System.Collections.IDictionary)
            return false;
        if (parameters is IReadOnlyDictionary<string, object?>)
            return false;
        if (parameters is System.Collections.IEnumerable && parameters is not string && parameters is not byte[])
            return false;

        var type = parameters.GetType();
        if (IsScalar(type))
            return true;

        // Fast lane is for one/two scalar anonymous parameters. Complex parameter objects stay on the compiled pipeline.
        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var count = 0;
        for (var i = 0; i < props.Length; i++)
        {
            if (props[i].GetIndexParameters().Length != 0 || props[i].GetMethod is null)
                continue;
            var pt = Nullable.GetUnderlyingType(props[i].PropertyType) ?? props[i].PropertyType;
            if (!IsScalar(pt))
                return false;
            count++;
            if (count > 2)
                return false;
        }
        return count <= 2;
    }

    public static async ValueTask<T?> FirstOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return default;

        var materializer = GetReader<T>(connection, sql, reader);
        return materializer(reader);
    }

    public static async ValueTask<T?> SingleOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return default;

        var materializer = GetReader<T>(connection, sql, reader);
        var first = materializer(reader);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Sequence contains more than one element.");
        return first;
    }

    public static async ValueTask<IReadOnlyList<T>> QueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        var materializer = GetReader<T>(connection, sql, reader);
        var rows = new List<T>(32);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            rows.Add(materializer(reader));
        return rows;
    }

    public static async ValueTask<T?> ScalarAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return ForgeScalarConverter.To<T>(value);
    }

    public static async ValueTask<int> ExecuteAsync(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DbCommand CreateCommand(DbConnection connection, string sql, object? parameters, DbTransaction? transaction, int? timeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        if (transaction is not null)
            command.Transaction = transaction;
        if (timeoutSeconds.HasValue)
            command.CommandTimeout = timeoutSeconds.Value;
        Bind(command, sql, parameters);
        return command;
    }

    private static void Bind(DbCommand command, string sql, object? parameters)
    {
        if (parameters is null)
            return;

        var firstName = FirstParameterName(sql);
        var type = parameters.GetType();
        if (IsScalar(type))
        {
            Add(command, firstName, parameters, type);
            return;
        }

        var accessor = ParameterAccessors.GetOrAdd(new DirectParameterKey(type, ForgeFastHash.HashSql(sql)), static key => BuildAccessor(key.ParameterType));
        accessor.Bind(command, parameters);
    }

    private static DirectParameterAccessor BuildAccessor(Type parameterType)
    {
        var props = parameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var selected = new List<PropertyInfo>(2);
        for (var i = 0; i < props.Length; i++)
        {
            if (props[i].GetIndexParameters().Length == 0 && props[i].GetMethod is not null)
                selected.Add(props[i]);
        }

        var names = new string[selected.Count];
        var types = new Type[selected.Count];
        var getters = new Func<object, object?>[selected.Count];
        for (var i = 0; i < selected.Count; i++)
        {
            names[i] = selected[i].Name;
            types[i] = selected[i].PropertyType;
            getters[i] = CompileGetter(parameterType, selected[i]);
        }
        return new DirectParameterAccessor(names, types, getters);
    }

    private static Func<object, object?> CompileGetter(Type declaringType, PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var cast = Expression.Convert(instance, declaringType);
        var access = Expression.Property(cast, property);
        var box = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<object, object?>>(box, instance).Compile();
    }

    private static void Add(DbCommand command, string name, object? value, Type? declaredType)
    {
        var p = command.CreateParameter();
        p.ParameterName = NormalizeParameterName(name);
        var dbType = ToDbType(declaredType ?? value?.GetType());
        if (dbType.HasValue)
            p.DbType = dbType.Value;

        if (value is null)
        {
            p.Value = DBNull.Value;
        }
        else
        {
            var type = Nullable.GetUnderlyingType(declaredType ?? value.GetType()) ?? (declaredType ?? value.GetType());
            p.Value = type.IsEnum
                ? Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture) ?? DBNull.Value
                : value;
        }
        command.Parameters.Add(p);
    }

    private static Func<DbDataReader, T> GetReader<T>(DbConnection connection, string sql, DbDataReader reader)
    {
        var key = new DirectReaderKey(connection.GetType(), typeof(T), ForgeFastHash.HashSql(sql));
        if (ReaderCache.TryGetValue(key, out var cached))
            return (Func<DbDataReader, T>)cached;
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        ReaderCache[key] = materializer;
        return materializer;
    }

    private static string FirstParameterName(string sql)
    {
        for (var i = 0; i < sql.Length - 1; i++)
        {
            var marker = sql[i];
            if (marker is not '@' and not ':')
                continue;
            var start = i + 1;
            if (start >= sql.Length || !(char.IsLetter(sql[start]) || sql[start] == '_'))
                continue;
            var end = start + 1;
            while (end < sql.Length && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_'))
                end++;
            return sql[start..end];
        }
        return "Value";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string NormalizeParameterName(string name)
        => name.Length > 0 && (name[0] == '@' || name[0] == ':') ? name : "@" + name;

    private static DbType? ToDbType(Type? type)
    {
        if (type is null)
            return null;
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);

        if (type == typeof(int)) return DbType.Int32;
        if (type == typeof(long)) return DbType.Int64;
        if (type == typeof(short)) return DbType.Int16;
        if (type == typeof(byte)) return DbType.Byte;
        if (type == typeof(bool)) return DbType.Boolean;
        if (type == typeof(string)) return DbType.String;
        if (type == typeof(decimal)) return DbType.Decimal;
        if (type == typeof(double)) return DbType.Double;
        if (type == typeof(float)) return DbType.Single;
        if (type == typeof(Guid)) return DbType.Guid;
        if (type == typeof(DateTime)) return DbType.DateTime2;
        if (type == typeof(DateTimeOffset)) return DbType.DateTimeOffset;
        if (type == typeof(TimeSpan)) return DbType.Time;
        if (type == typeof(byte[])) return DbType.Binary;
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly)
            || type == typeof(TimeSpan) || type == typeof(byte[]);
    }

    private readonly record struct DirectParameterKey(Type ParameterType, ulong SqlHash);
    private readonly record struct DirectReaderKey(Type ProviderType, Type ResultType, ulong SqlHash);

    private sealed class DirectParameterAccessor
    {
        private readonly Func<object, object?>[] _getters;
        private readonly string[] _names;
        private readonly Type[] _types;

        public DirectParameterAccessor(string[] names, Type[] types, Func<object, object?>[] getters)
        {
            _names = names;
            _types = types;
            _getters = getters;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(DbCommand command, object parameters)
        {
            for (var i = 0; i < _getters.Length; i++)
                Add(command, _names[i], _getters[i](parameters), _types[i]);
        }
    }
}
