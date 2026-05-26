using System.Collections;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

public static class ForgeAdo
{
    private static readonly ConcurrentDictionary<Type, ParameterProperty[]> ParameterPropertyCache = new();
    private static readonly ConcurrentDictionary<Type, Action<DbCommand, object>> ParameterWriterCache = new();
    private static readonly ConcurrentDictionary<string, string[]> SqlParameterTokenCache = new(StringComparer.Ordinal);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public static IReadOnlyList<T> Query<T>(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => QueryAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async ValueTask<IReadOnlyList<T>> QueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return await ForgePerformancePipeline.QueryAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async ValueTask<T?> QueryFirstOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return await ForgePerformancePipeline.FirstOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<T?> QuerySingleOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return await ForgePerformancePipeline.SingleOrDefaultAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)
            .ConfigureAwait(false);
    }
    /// <summary>
    /// Executes the Execute operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Execute operation.</returns>
    public static int Execute(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
      => ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();
    /// <summary>
    /// Executes the ExecuteAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    public static async ValueTask<int> ExecuteAsync(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return await ForgePerformancePipeline.ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds, cancellationToken)
            .ConfigureAwait(false);
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    public static T? ExecuteScalar<T>(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
      => ExecuteScalarAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public static async ValueTask<T?> ExecuteScalarAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);
        var value = await command.ExecuteScalarAsync(cancellationToken);

        return ForgeValueConverter.FromDatabase<T>(value);
    }

    /// <summary>
    /// Executes the CreateCommand operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the CreateCommand operation.</returns>
    public static DbCommand CreateCommand(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null)
    {
        var providerName = connection.GetType().FullName ?? connection.GetType().Name;
        var plan = ForgePerformanceCommandPlanCache.GetOrAdd(providerName, sql, commandType, parameters?.GetType());
        var command = ForgePerformanceCommandPlanCache.CreateCommand(connection, plan, transaction, timeoutSeconds);

        BindParameters(command, parameters);
        ValidateNoUnboundSqlParameters(command);

        return command;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void BindParameters(DbCommand command, object? parameters)
    {
        if (parameters is null)
            return;

        // Defensive guard: a CancellationToken must never be treated as SQL parameters.
        // This prevents provider errors such as ManualResetEvent being bound as a parameter
        // when callers accidentally pass ct in the parameters position.
        if (parameters is CancellationToken)
            return;

        // Scalar parameter hot path. This fixes calls like GetByIdAsync<T>(id) where the SQL
        // contains @Id but the parameter value is a raw int/Guid/string instead of an anonymous object.
        if (IsScalarParameterType(parameters.GetType()))
        {
            BindScalarParameter(command, parameters);
            return;
        }

        if (parameters is IReadOnlyDictionary<string, object?> dictionary)
        {
            foreach (var item in dictionary)
                BindSingleOrEnumerable(command, item.Key, item.Value, item.Value?.GetType());

            EnsureReferencedSqlParametersAreBound(command, parameters);
            return;
        }

        if (parameters is IDictionary nonGenericDictionary)
        {
            foreach (DictionaryEntry item in nonGenericDictionary)
            {
                if (item.Key is null)
                    continue;

                var key = Convert.ToString(item.Key, System.Globalization.CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                BindSingleOrEnumerable(command, key, item.Value, item.Value?.GetType());
            }

            EnsureReferencedSqlParametersAreBound(command, parameters);
            return;
        }

        // Primary fast path: cached MSIL writer for anonymous objects / POCO parameter bags.
        var writer = ParameterWriterCache.GetOrAdd(parameters.GetType(), BuildParameterWriter);
        writer(command, parameters);

        // Safety net: make sure every @Parameter used by SQL is actually bound.
        // This fixes cases like GetByIdAsync where optimized pipeline generated SQL with @Id
        // but a parameter bag was not bound due to anonymous type / dictionary edge cases.
        EnsureReferencedSqlParametersAreBound(command, parameters);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BindScalarParameter(DbCommand command, object value)
    {
        var names = command.CommandType == CommandType.Text && !string.IsNullOrWhiteSpace(command.CommandText)
            ? SqlParameterTokenCache.GetOrAdd(command.CommandText, ExtractSqlParameterNames)
            : Array.Empty<string>();

        if (names.Length == 0)
        {
            BindSingleOrEnumerable(command, "Value", value, value.GetType());
            return;
        }

        // Bind every referenced token to the scalar value. This keeps GetById (@Id),
        // graph delete (@ParentId), and provider-specific single-value commands safe.
        foreach (var name in names)
        {
            if (!HasParameter(command, name))
                BindSingleOrEnumerable(command, name, value, value.GetType());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsScalarParameterType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(Guid)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(DateOnly)
               || type == typeof(TimeOnly)
               || type == typeof(TimeSpan)
               || type == typeof(byte[]);
    }

    private static void EnsureReferencedSqlParametersAreBound(DbCommand command, object parameters)
    {
        if (command.CommandType != CommandType.Text)
            return;

        if (string.IsNullOrWhiteSpace(command.CommandText))
            return;

        var names = SqlParameterTokenCache.GetOrAdd(command.CommandText, ExtractSqlParameterNames);

        if (names.Length == 0)
            return;

        var props = ParameterPropertyCache.GetOrAdd(parameters.GetType(), BuildParameterProperties);

        foreach (var name in names)
        {

            if (HasParameter(command, name))
                continue;

            ParameterProperty? prop = null;
            for (var i = 0; i < props.Length; i++)
            {
                if (string.Equals(props[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    prop = props[i];
                    break;
                }
            }

            if (prop is null)
                continue;

            var value = prop.Getter(parameters);
            BindSingleOrEnumerable(command, name, value, prop.PropertyType);
        }
    }


    private static void ValidateNoUnboundSqlParameters(DbCommand command)
    {
        if (command.CommandType != CommandType.Text || string.IsNullOrWhiteSpace(command.CommandText))
            return;

        var names = SqlParameterTokenCache.GetOrAdd(command.CommandText, ExtractSqlParameterNames);
        if (names.Length == 0)
            return;

        foreach (var name in names)
        {
            if (!HasParameter(command, name))
            {
                throw new InvalidOperationException(
                    $"ForgeORM command is missing SQL parameter '@{name.TrimStart('@', ':')}'. " +
                    "Pass a scalar value for single-token SQL, or pass an anonymous object/dictionary with a matching property/key.");
            }
        }
    }

    private static string[] ExtractSqlParameterNames(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return Array.Empty<string>();

        var names = new List<string>(4);

        for (var i = 0; i < sql.Length - 1; i++)
        {
            if (sql[i] != '@')
                continue;

            if (i > 0 && sql[i - 1] == '@')
                continue;

            var start = i + 1;
            if (start >= sql.Length || !(char.IsLetter(sql[start]) || sql[start] == '_'))
                continue;

            var end = start + 1;
            while (end < sql.Length && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_'))
                end++;

            var name = sql[start..end];
            var exists = false;
            for (var n = 0; n < names.Count; n++)
            {
                if (string.Equals(names[n], name, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
                names.Add(name);

            i = end - 1;
        }

        return names.Count == 0 ? Array.Empty<string>() : names.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasParameter(DbCommand command, string name)
    {
        var normalized = name.TrimStart('@', ':');

        foreach (DbParameter parameter in command.Parameters)
        {
            var current = parameter.ParameterName.TrimStart('@', ':');
            if (string.Equals(current, normalized, StringComparison.OrdinalIgnoreCase) || IsExpandedParameterName(current, normalized))
                return true;
        }

        return false;
    }

    private static bool IsExpandedParameterName(string current, string logicalName)
    {
        if (!current.StartsWith(logicalName, StringComparison.OrdinalIgnoreCase))
            return false;
        if (current.Length == logicalName.Length)
            return true;
        var suffixStart = current[logicalName.Length];
        return char.IsDigit(suffixStart) || suffixStart == '_';
    }

    private static void BindSingleOrEnumerable(
        DbCommand command,
        string name,
        object? value,
        Type? declaredType)
    {
        if (IsEnumerableParameter(value))
        {
            ExpandEnumerableParameter(command, name, (IEnumerable)value!);
            return;
        }

        AddParameter(command, name, ForgeValueConverter.ToDatabase(value, declaredType));
    }

    private static void ExpandEnumerableParameter(
        DbCommand command,
        string name,
        IEnumerable values)
    {
        var cleanName = name.TrimStart('@', ':');
        RemoveParameterFamily(command, cleanName);

        var parameterNames = new List<string>();
        var index = 0;

        foreach (var value in values)
        {
            string parameterName;
            do
            {
                parameterName = $"{cleanName}{index++}";
            }
            while (HasExactParameter(command, parameterName));

            parameterNames.Add("@" + parameterName);
            AddParameter(command, parameterName, ForgeValueConverter.ToDatabase(value, value?.GetType()));
        }

        var replacement = parameterNames.Count == 0
            ? "(NULL)"
            : "(" + string.Join(", ", parameterNames) + ")";

        command.CommandText = ReplaceParameterToken(command.CommandText, cleanName, replacement);
    }

    private static void RemoveParameterFamily(DbCommand command, string logicalName)
    {
        var normalized = logicalName.TrimStart('@', ':');
        for (var i = command.Parameters.Count - 1; i >= 0; i--)
        {
            if (command.Parameters[i] is not DbParameter parameter)
                continue;

            var current = parameter.ParameterName.TrimStart('@', ':');
            if (string.Equals(current, normalized, StringComparison.OrdinalIgnoreCase) || IsExpandedParameterName(current, normalized))
                command.Parameters.RemoveAt(i);
        }
    }

    private static bool HasExactParameter(DbCommand command, string name)
    {
        var normalized = name.TrimStart('@', ':');
        foreach (DbParameter parameter in command.Parameters)
        {
            if (string.Equals(parameter.ParameterName.TrimStart('@', ':'), normalized, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string ReplaceParameterToken(string sql, string parameterName, string replacement)
    {
        return ReplaceToken(ReplaceToken(sql, "@" + parameterName, replacement), ":" + parameterName, replacement);
    }

    private static string ReplaceToken(string sql, string token, string replacement)
    {
        var index = 0;
        System.Text.StringBuilder? builder = null;
        var lastCopyIndex = 0;

        while ((index = sql.IndexOf(token, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = index + token.Length;
            if (end < sql.Length && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_'))
            {
                index = end;
                continue;
            }

            builder ??= new System.Text.StringBuilder(sql.Length + replacement.Length);
            builder.Append(sql, lastCopyIndex, index - lastCopyIndex);
            builder.Append(replacement);
            lastCopyIndex = end;
            index = end;
        }

        if (builder is null)
            return sql;

        builder.Append(sql, lastCopyIndex, sql.Length - lastCopyIndex);
        return builder.ToString();
    }

    private static DbParameter? FindExactParameter(DbCommand command, string name)
    {
        var normalized = name.TrimStart('@', ':');
        foreach (DbParameter parameter in command.Parameters)
        {
            if (string.Equals(parameter.ParameterName.TrimStart('@', ':'), normalized, StringComparison.OrdinalIgnoreCase))
                return parameter;
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddParameter(DbCommand command, string name, object? value)
    {
        name = name.TrimStart('@', ':');

        var parameter = FindExactParameter(command, name);
        if (parameter is null)
        {
            parameter = command.CreateParameter();
            parameter.ParameterName = "@" + name;
            command.Parameters.Add(parameter);
        }

        parameter.Value = NormalizeParameterValue(value) ?? DBNull.Value;

        if (value is DateTime)
            parameter.DbType = DbType.DateTime2;
        else if (value is DateTimeOffset)
            parameter.DbType = DbType.DateTimeOffset;
    }

    private static Action<DbCommand, object> BuildParameterWriter(Type type)
    {
        if (ForgeSourceGeneratedRegistry.CompilationMode != ForgeOrmCompilationMode.RuntimeEmit
            && ForgeSourceGeneratedRegistry.TryGetProvider(type, out var provider)
            && provider.TryGetBinder(type, out var generatedBinder)
            && generatedBinder is not null)
            return generatedBinder;

        if (ForgeSourceGeneratedRegistry.CompilationMode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            throw new InvalidOperationException($"No ForgeORM source-generated parameter binder was registered for {type.FullName}.");

        var props = ParameterPropertyCache.GetOrAdd(type, BuildParameterProperties);

        var method = new DynamicMethod(
            $"ForgeORM_BindParameters_{type.Name}",
            typeof(void),
            new[] { typeof(DbCommand), typeof(object) },
            typeof(ForgeAdo),
            skipVisibility: true);

        var il = method.GetILGenerator();
        var typed = il.DeclareLocal(type);

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(type.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, type);
        il.Emit(OpCodes.Stloc, typed);

        var bind = typeof(ForgeAdo).GetMethod(
            nameof(BindSingleOrEnumerable),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var getTypeFromHandle = typeof(Type).GetMethod(
            nameof(Type.GetTypeFromHandle),
            new[] { typeof(RuntimeTypeHandle) })!;

        foreach (var prop in props)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, prop.Name);
            il.Emit(OpCodes.Ldloc, typed);
            il.Emit(OpCodes.Callvirt, prop.Property.GetMethod!);

            if (prop.PropertyType.IsValueType)
                il.Emit(OpCodes.Box, prop.PropertyType);

            il.Emit(OpCodes.Ldtoken, prop.PropertyType);
            il.Emit(OpCodes.Call, getTypeFromHandle);
            il.Emit(OpCodes.Call, bind);
        }

        il.Emit(OpCodes.Ret);

        return (Action<DbCommand, object>)method.CreateDelegate(typeof(Action<DbCommand, object>));
    }

    private static ParameterProperty[] BuildParameterProperties(Type type)
    {
        var source = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (source.Length == 0)
            return Array.Empty<ParameterProperty>();

        var buffer = new ParameterProperty[source.Length];
        var count = 0;
        for (var i = 0; i < source.Length; i++)
        {
            var property = source[i];
            if (!property.CanRead || !IsBindableParameterProperty(property))
                continue;

            buffer[count++] = new ParameterProperty(
                property.Name,
                property.PropertyType,
                property,
                BuildParameterGetter(type, property));
        }

        if (count == 0)
            return Array.Empty<ParameterProperty>();

        if (count == buffer.Length)
            return buffer;

        var result = new ParameterProperty[count];
        Array.Copy(buffer, result, count);
        return result;
    }

    private static Func<object, object?> BuildParameterGetter(Type declaringType, PropertyInfo property)
        => ForgeRuntimeAccessorCache.Getter(property);

    private static bool IsBindableParameterProperty(PropertyInfo property)
    {
        if (!property.CanRead)
            return false;

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (type == typeof(string) || type == typeof(byte[]))
            return true;

        if (typeof(IEnumerable).IsAssignableFrom(type))
            return false;

        return ForgeMaterializer.IsScalar(property.PropertyType);
    }

    private static int EstimateCapacity(string sql)
    {
        if (TryReadPositiveIntAfter(sql.AsSpan(), "FETCH NEXT".AsSpan(), out var take))
            return Math.Clamp(take, 4, 1024);

        if (TryReadPositiveIntAfter(sql.AsSpan(), "TOP".AsSpan(), out var topCount))
            return Math.Clamp(topCount, 1, 1024);

        if (TryReadPositiveIntAfter(sql.AsSpan(), "LIMIT".AsSpan(), out var limit))
            return Math.Clamp(limit, 4, 1024);

        return 16;
    }

    private static bool TryReadPositiveIntAfter(ReadOnlySpan<char> source, ReadOnlySpan<char> token, out int value)
    {
        value = 0;
        var index = source.IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return false;

        index += token.Length;
        while (index < source.Length && (char.IsWhiteSpace(source[index]) || source[index] == '('))
            index++;

        var start = index;
        while (index < source.Length && char.IsDigit(source[index]))
        {
            value = checked((value * 10) + (source[index] - '0'));
            index++;
        }

        return index > start && value > 0;
    }

    private static object? NormalizeParameterValue(object? value)
    {
        // Default ForgeORM enum parameter behavior uses string storage because raw SQL and
        // sample schemas commonly store enum columns as NVARCHAR values such as 'Paid'.
        // Numeric enum storage remains supported through ForgeValueConverter/explicit mapping paths.
        if (value is Enum enumValue)
        {
            return enumValue.ToString();
        }

        if (value is DateTime dateTime)
            return dateTime == default || dateTime < new DateTime(1753, 1, 1) ? DateTime.UtcNow : dateTime;

        if (value is DateTimeOffset dateTimeOffset)
            return dateTimeOffset == default ? DateTimeOffset.UtcNow : dateTimeOffset;

        return value;
    }

    private sealed record ParameterProperty(string Name, Type PropertyType, PropertyInfo Property, Func<object, object?> Getter);

    private static bool IsEnumerableParameter(object? value)
    {
        if (value is null)
            return false;

        if (value is string or byte[])
            return false;

        return value is IEnumerable;
    }

    /// <summary>
    /// Executes the QueryDynamicAsync operation.
    /// </summary>
    /// <param name="connection">The connection value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="transaction">The transaction value.</param>
    /// <param name="commandType">The commandType value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the QueryDynamicAsync operation.</returns>
    public static async ValueTask<IReadOnlyList<IDictionary<string, object?>>> QueryDynamicAsync(
    DbConnection connection,
    string sql,
    object? parameters = null,
    DbTransaction? transaction = null,
    CommandType commandType = CommandType.Text,
    int? timeoutSeconds = null,
    CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(
            connection,
            sql,
            parameters,
            transaction,
            commandType,
            timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

        var rows = new List<IDictionary<string, object?>>(EstimateCapacity(sql));

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i)
                    ? null
                    : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return rows;
    }
}