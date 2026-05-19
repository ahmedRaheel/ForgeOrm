using System.Collections;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;

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
    public static async Task<IReadOnlyList<T>> QueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        _ = ForgeCompiledQueryCache.GetOrAdd(connection.GetType().Name, typeof(T), sql, parameters?.GetType(), () => new ForgeCompiledQueryPlan(sql, typeof(T), parameters?.GetType(), connection.GetType().Name, ForgeCompiledQueryCache.Fingerprint(sql)));
        _ = ForgeCompiledQueryCache.GetOrAdd(connection.GetType().Name, typeof(T), sql, parameters?.GetType(), () => new ForgeCompiledQueryPlan(sql, typeof(T), parameters?.GetType(), connection.GetType().Name, ForgeCompiledQueryCache.Fingerprint(sql)));
        await using var command = CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

        var rows = new List<T>(EstimateCapacity(sql));
        var materializer = ForgeIlMaterializerCache.GetOrCreate<T>(reader);

        while (await reader.ReadAsync(cancellationToken))
            rows.Add(materializer(reader));

        return rows;
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
    public static async Task<T?> QueryFirstOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess, cancellationToken);
        var materializer = ForgeIlMaterializerCache.GetOrCreate<T>(reader);

        return await reader.ReadAsync(cancellationToken)
            ? materializer(reader)
            : default;
    }

    public static async Task<T?> QuerySingleOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return default;

        var materializer = ForgeIlMaterializerCache.GetOrCreate<T>(reader);
        var first = materializer(reader);

        if (await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Sequence contains more than one element.");

        return first;
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
    public static async Task<int> ExecuteAsync(
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
        return await command.ExecuteNonQueryAsync(cancellationToken);
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
    public static async Task<T?> ExecuteScalarAsync<T>(
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
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = commandType;

        if (timeoutSeconds.HasValue)
            command.CommandTimeout = timeoutSeconds.Value;

        if (transaction is not null)
            command.Transaction = transaction;

        BindParameters(command, parameters);

        return command;
    }

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

            var prop = props.FirstOrDefault(x =>
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            if (prop is null || prop.Property is null)
                continue;

            var value = prop.Getter(parameters);
            BindSingleOrEnumerable(command, name, value, prop.PropertyType);
        }
    }


    private static string[] ExtractSqlParameterNames(string sql)
    {
        var matches = Regex.Matches(
            sql,
            @"(?<!@)@([A-Za-z_][A-Za-z0-9_]*)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        if (matches.Count == 0)
            return Array.Empty<string>();

        var result = new string[matches.Count];
        for (var i = 0; i < matches.Count; i++)
            result[i] = matches[i].Groups[1].Value;
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool HasParameter(DbCommand command, string name)
    {
        var expected = "@" + name.TrimStart('@', ':');

        foreach (DbParameter parameter in command.Parameters)
        {
            if (string.Equals(parameter.ParameterName, expected, StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(parameter.ParameterName.TrimStart('@', ':'), name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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
        var parameterNames = new List<string>();
        var index = 0;

        foreach (var value in values)
        {
            var parameterName = $"{cleanName}{index++}";
            parameterNames.Add("@" + parameterName);
            AddParameter(command, parameterName, ForgeValueConverter.ToDatabase(value, value?.GetType()));
        }

        var replacement = parameterNames.Count == 0
            ? "(NULL)"
            : "(" + string.Join(", ", parameterNames) + ")";

        command.CommandText = ReplaceParameterToken(command.CommandText, cleanName, replacement);
    }

    private static string ReplaceParameterToken(string sql, string parameterName, string replacement)
    {
        sql = sql.Replace("@" + parameterName, replacement, StringComparison.OrdinalIgnoreCase);
        sql = sql.Replace(":" + parameterName, replacement, StringComparison.OrdinalIgnoreCase);
        return sql;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        name = name.TrimStart('@', ':');

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@" + name;
        parameter.Value = NormalizeParameterValue(value) ?? DBNull.Value;

        if (value is DateTime)
            parameter.DbType = DbType.DateTime2;
        else if (value is DateTimeOffset)
            parameter.DbType = DbType.DateTimeOffset;

        command.Parameters.Add(parameter);
    }

    private static Action<DbCommand, object> BuildParameterWriter(Type type)
    {
        if (ForgeSourceGeneratedRegistry.CompilationMode != ForgeOrmCompilationMode.RuntimeEmit
            && ForgeSourceGeneratedRegistry.TryGetProvider(type, out var provider))
            return provider.GetBinder(type);

        if (ForgeSourceGeneratedRegistry.CompilationMode == ForgeOrmCompilationMode.SourceGenerated)
            throw new InvalidOperationException($"No ForgeORM source-generated parameter binder was registered for {type.FullName}.");

        var props = ParameterPropertyCache.GetOrAdd(type, BuildParameterProperties);

        var method = new DynamicMethod(
            $"ForgeORM_BindParameters_{type.Name}_{Guid.NewGuid():N}",
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
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsBindableParameterProperty(p))
            .Select(p => new ParameterProperty(p.Name, p.PropertyType, p, BuildParameterGetter(type, p)))
            .ToArray();
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
        var match = Regex.Match(sql, @"FETCH\s+NEXT\s+(\d+)\s+ROWS", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var take))
            return Math.Clamp(take, 4, 1024);

        var top = Regex.Match(sql, @"TOP\s*\(?\s*(\d+)\s*\)?", RegexOptions.IgnoreCase);
        if (top.Success && int.TryParse(top.Groups[1].Value, out var topCount))
            return Math.Clamp(topCount, 1, 1024);

        return 16;
    }

    private static object? NormalizeParameterValue(object? value)
    {
        if (value is Enum enumValue)
        {
            var underlying = Enum.GetUnderlyingType(enumValue.GetType());
            return Convert.ChangeType(enumValue, underlying);
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
    public static async Task<IReadOnlyList<IDictionary<string, object?>>> QueryDynamicAsync(
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