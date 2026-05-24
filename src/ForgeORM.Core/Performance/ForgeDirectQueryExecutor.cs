using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ForgeORM.Core;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Provider-neutral Dapper-style direct executor used by the framework-level pipeline for simple hot paths.
/// Query/First/Single/Scalar/Execute all enter through the same execution policy and this executor is selected
/// internally when the command shape is safe. Complex shapes still fall back to the compiled pipeline.
/// </summary>
internal static class ForgeDirectQueryExecutor
{
    private static readonly ConcurrentDictionary<Type, DirectParameterAccessor?> ParameterAccessorsByType = new();
    private static readonly ConcurrentDictionary<string, string> FirstParameterNameBySql = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<DirectReaderKey, object> ReaderCache = new();
    private static readonly ConcurrentDictionary<DirectPlanKey, DirectExecutionPlan> PlanCache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanUse(string sql, object? parameters, CommandType commandType)
        => TryGetPlan(sql, parameters, commandType, out _);

    /// <summary>Precompiles the direct execution plan for a command shape during application/benchmark warmup.</summary>
    public static bool Precompile(string sql, object? parameters = null, CommandType commandType = CommandType.Text)
        => TryGetPlan(sql, parameters, commandType, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryQueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<IReadOnlyList<T>> result)
    {
        if (!TryGetPlan(sql, parameters, commandType, out var plan))
        {
            result = default;
            return false;
        }

        result = QueryCoreAsync<T>(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFirstOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        if (!TryGetPlan(sql, parameters, commandType, out var plan))
        {
            result = default;
            return false;
        }

        result = FirstOrDefaultCoreAsync<T>(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TrySingleOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        if (!TryGetPlan(sql, parameters, commandType, out var plan))
        {
            result = default;
            return false;
        }

        result = SingleOrDefaultCoreAsync<T>(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryScalarAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<T?> result)
    {
        if (!TryGetPlan(sql, parameters, commandType, out var plan))
        {
            result = default;
            return false;
        }

        result = ScalarCoreAsync<T>(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryExecuteAsync(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        CommandType commandType,
        int? timeoutSeconds,
        CancellationToken cancellationToken,
        out ValueTask<int> result)
    {
        if (!TryGetPlan(sql, parameters, commandType, out var plan))
        {
            result = default;
            return false;
        }

        result = ExecuteCoreAsync(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
        return true;
    }

    public static ValueTask<T?> FirstOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var plan = GetRequiredPlan(sql, parameters, CommandType.Text);
        return FirstOrDefaultCoreAsync<T>(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
    }

    public static ValueTask<T?> SingleOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var plan = GetRequiredPlan(sql, parameters, CommandType.Text);
        return SingleOrDefaultCoreAsync<T>(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
    }

    public static ValueTask<IReadOnlyList<T>> QueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var plan = GetRequiredPlan(sql, parameters, CommandType.Text);
        return QueryCoreAsync<T>(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
    }

    public static async IAsyncEnumerable<T> StreamAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var plan = GetRequiredPlan(sql, parameters, CommandType.Text);
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds, plan);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        var materializer = GetReader<T>(connection, sql, reader);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return materializer(reader);
    }

    public static ValueTask<T?> ScalarAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var plan = GetRequiredPlan(sql, parameters, CommandType.Text);
        return ScalarCoreAsync<T>(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
    }

    public static ValueTask<int> ExecuteAsync(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var plan = GetRequiredPlan(sql, parameters, CommandType.Text);
        return ExecuteCoreAsync(connection, sql, parameters, transaction, timeoutSeconds, plan, cancellationToken);
    }

    private static async ValueTask<T?> FirstOrDefaultCoreAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        DirectExecutionPlan plan,
        CancellationToken cancellationToken)
    {
        ForgeDirectExecutionDiagnostics.HitFirst();
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds, plan);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return default;

        return GetReader<T>(connection, sql, reader)(reader);
    }

    private static async ValueTask<T?> SingleOrDefaultCoreAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        DirectExecutionPlan plan,
        CancellationToken cancellationToken)
    {
        ForgeDirectExecutionDiagnostics.HitSingle();
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds, plan);
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

    private static async ValueTask<IReadOnlyList<T>> QueryCoreAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        DirectExecutionPlan plan,
        CancellationToken cancellationToken)
    {
        ForgeDirectExecutionDiagnostics.HitQuery();
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds, plan);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        var materializer = GetReader<T>(connection, sql, reader);
        var rows = new List<T>(32);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            rows.Add(materializer(reader));

        return rows;
    }

    private static async ValueTask<T?> ScalarCoreAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        DirectExecutionPlan plan,
        CancellationToken cancellationToken)
    {
        ForgeDirectExecutionDiagnostics.HitScalar();
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds, plan);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return ForgeScalarConverter.To<T>(value);
    }

    private static async ValueTask<int> ExecuteCoreAsync(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        DirectExecutionPlan plan,
        CancellationToken cancellationToken)
    {
        ForgeDirectExecutionDiagnostics.HitExecute();
        await using var command = CreateCommand(connection, sql, parameters, transaction, timeoutSeconds, plan);
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DirectExecutionPlan GetRequiredPlan(string sql, object? parameters, CommandType commandType)
        => TryGetPlan(sql, parameters, commandType, out var plan)
            ? plan
            : throw new InvalidOperationException("Direct executor cannot handle this command shape.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetPlan(string sql, object? parameters, CommandType commandType, out DirectExecutionPlan plan)
    {
        if (ForgeEnterpriseRuntime.IsEnabled || commandType != CommandType.Text || string.IsNullOrWhiteSpace(sql))
        {
            ForgeDirectExecutionDiagnostics.Reject();
            plan = DirectExecutionPlan.Unsupported;
            return false;
        }

        var parameterType = parameters?.GetType();
        var key = new DirectPlanKey(sql, parameterType, commandType);
        plan = PlanCache.GetOrAdd(key, static (k, args) => BuildPlan(k.Sql, args.Parameters), new BuildPlanArgs(parameters));
        if (plan.IsSupported)
            return true;

        ForgeDirectExecutionDiagnostics.Reject();
        return false;
    }

    private static DirectExecutionPlan BuildPlan(string sql, object? parameters)
    {
        if (parameters is null)
            return DirectExecutionPlan.NoParameters;

        if (parameters is System.Collections.IDictionary || parameters is IReadOnlyDictionary<string, object?>)
            return DirectExecutionPlan.Unsupported;

        if (parameters is System.Collections.IEnumerable && parameters is not string && parameters is not byte[])
            return DirectExecutionPlan.Unsupported;

        var type = parameters.GetType();
        if (IsScalar(type))
            return DirectExecutionPlan.ForScalar(NormalizeParameterName(FirstParameterName(sql)), type);

        var accessor = GetAccessor(type);
        return accessor is null ? DirectExecutionPlan.Unsupported : DirectExecutionPlan.ForAccessor(accessor);
    }

    private static DbCommand CreateCommand(
        DbConnection connection,
        string sql,
        object? parameters,
        DbTransaction? transaction,
        int? timeoutSeconds,
        DirectExecutionPlan plan)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        if (transaction is not null)
            command.Transaction = transaction;
        if (timeoutSeconds.HasValue)
            command.CommandTimeout = timeoutSeconds.Value;
        plan.Bind(command, parameters);
        return command;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DirectParameterAccessor? GetAccessor(Type parameterType)
        => ParameterAccessorsByType.GetOrAdd(parameterType, static type => BuildAccessor(type));

    private static DirectParameterAccessor? BuildAccessor(Type parameterType)
    {
        var props = parameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var selected = new List<PropertyInfo>(2);
        for (var i = 0; i < props.Length; i++)
        {
            var prop = props[i];
            if (prop.GetIndexParameters().Length != 0 || prop.GetMethod is null)
                continue;

            var propertyType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (!IsScalar(propertyType))
                return null;

            selected.Add(prop);
            if (selected.Count > 2)
                return null;
        }

        if (selected.Count == 0)
            return null;

        var names = new string[selected.Count];
        var types = new Type[selected.Count];
        var getters = new Func<object, object?>[selected.Count];
        for (var i = 0; i < selected.Count; i++)
        {
            names[i] = NormalizeParameterName(selected[i].Name);
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
        var parameterValue = NormalizeValue(value, declaredType);
        var effectiveType = declaredType ?? value?.GetType();

        if (command is SqlCommand sqlCommand)
        {
            var sqlType = ToSqlDbType(effectiveType);
            var sqlParameter = sqlType.HasValue
                ? sqlCommand.Parameters.Add(name, sqlType.Value)
                : sqlCommand.Parameters.Add(name, SqlDbType.Variant);
            sqlParameter.Value = parameterValue;
            return;
        }

        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        var dbType = ToDbType(effectiveType);
        if (dbType.HasValue)
            parameter.DbType = dbType.Value;
        parameter.Value = parameterValue;
        command.Parameters.Add(parameter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object NormalizeValue(object? value, Type? declaredType)
    {
        if (value is null)
            return DBNull.Value;

        var type = Nullable.GetUnderlyingType(declaredType ?? value.GetType()) ?? (declaredType ?? value.GetType());
        return type.IsEnum
            ? Convert.ChangeType(value, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture) ?? DBNull.Value
            : value;
    }

    private static Func<DbDataReader, T> GetReader<T>(DbConnection connection, string sql, DbDataReader reader)
    {
        var key = new DirectReaderKey(connection.GetType(), typeof(T), sql);
        if (ReaderCache.TryGetValue(key, out var cached))
            return (Func<DbDataReader, T>)cached;

        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        ReaderCache[key] = materializer;
        return materializer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FirstParameterName(string sql)
        => FirstParameterNameBySql.GetOrAdd(sql, static s => ParseFirstParameterName(s));

    private static string ParseFirstParameterName(string sql)
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

    private static SqlDbType? ToSqlDbType(Type? type)
    {
        if (type is null)
            return null;

        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);

        if (type == typeof(int)) return SqlDbType.Int;
        if (type == typeof(long)) return SqlDbType.BigInt;
        if (type == typeof(short)) return SqlDbType.SmallInt;
        if (type == typeof(byte)) return SqlDbType.TinyInt;
        if (type == typeof(bool)) return SqlDbType.Bit;
        if (type == typeof(string)) return SqlDbType.NVarChar;
        if (type == typeof(decimal)) return SqlDbType.Decimal;
        if (type == typeof(double)) return SqlDbType.Float;
        if (type == typeof(float)) return SqlDbType.Real;
        if (type == typeof(Guid)) return SqlDbType.UniqueIdentifier;
        if (type == typeof(DateTime)) return SqlDbType.DateTime2;
        if (type == typeof(DateTimeOffset)) return SqlDbType.DateTimeOffset;
        if (type == typeof(TimeSpan)) return SqlDbType.Time;
        if (type == typeof(byte[])) return SqlDbType.VarBinary;
        return null;
    }

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

    private readonly record struct DirectReaderKey(Type ProviderType, Type ResultType, string Sql);

    private readonly record struct DirectPlanKey(string Sql, Type? ParameterType, CommandType CommandType);

    private readonly record struct BuildPlanArgs(object? Parameters);

    private sealed class DirectExecutionPlan
    {
        public static readonly DirectExecutionPlan Unsupported = new(null, null, null, false);
        public static readonly DirectExecutionPlan NoParameters = new(null, null, null, true);

        private readonly DirectParameterAccessor? _accessor;
        private readonly string? _scalarName;
        private readonly Type? _scalarType;

        private DirectExecutionPlan(DirectParameterAccessor? accessor, string? scalarName, Type? scalarType, bool isSupported)
        {
            _accessor = accessor;
            _scalarName = scalarName;
            _scalarType = scalarType;
            IsSupported = isSupported;
        }

        public bool IsSupported { get; }

        public static DirectExecutionPlan ForScalar(string name, Type type) => new(null, name, type, true);

        public static DirectExecutionPlan ForAccessor(DirectParameterAccessor accessor) => new(accessor, null, null, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bind(DbCommand command, object? parameters)
        {
            if (parameters is null)
                return;

            if (_scalarName is not null)
            {
                Add(command, _scalarName, parameters, _scalarType);
                return;
            }

            _accessor!.Bind(command, parameters);
        }
    }

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
