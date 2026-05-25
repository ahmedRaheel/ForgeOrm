using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Final compiled query plan used by the high-performance execution pipeline.
/// SQL/command metadata and parameter layout are cached before execution; materializer is attached after reader shape is known.
/// </summary>
public sealed class ForgeCompiledQueryPlan<T>
{
    public required string Sql { get; init; }
    public required CommandType CommandType { get; init; }
    public required CommandBehavior Behavior { get; init; }
    public required Action<DbCommand, object?> Binder { get; init; }
    public required string[] ParameterNames { get; init; }
    public Func<DbDataReader, T>? Materializer { get; set; }
    public required string Provider { get; init; }
    public required Type? ParameterType { get; init; }
    public required string QueryFingerprint { get; init; }
    public required bool RequiresEnumNormalization { get; init; }
}

internal static class ForgeCompiledExecutionPlanCache
{
    private static readonly ConcurrentDictionary<ForgeCompiledExecutionPlanKey, object> Cache = new();

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static ForgeCompiledQueryPlan<T> GetOrAdd<T>(DbConnection connection, string sql, object? parameters, CommandType commandType, CommandBehavior behavior)
    {
        var provider = connection.GetType().FullName ?? connection.GetType().Name;
        var parameterType = parameters?.GetType();
        var sqlHash = ForgeFastHash.HashSql(sql);
        var key = new ForgeCompiledExecutionPlanKey(provider, typeof(T), parameterType, commandType, behavior, sqlHash);
        return (ForgeCompiledQueryPlan<T>)Cache.GetOrAdd(key, _ => new ForgeCompiledQueryPlan<T>
        {
            Sql = sql,
            CommandType = commandType,
            Behavior = behavior,
            Provider = provider,
            ParameterType = parameterType,
            QueryFingerprint = key.SqlFingerprint.ToString("X16", System.Globalization.CultureInfo.InvariantCulture),
            ParameterNames = ForgeParameterBinderCompiler.ExtractParameterNames(sql, commandType),
            Binder = ForgeParameterBinderCompiler.Compile(parameterType, sql, commandType, sqlHash),
            RequiresEnumNormalization = ForgeRawEnumSqlAnalyzer.RequiresNormalization<T>(sql, commandType)
        });
    }
}


internal static class ForgeRawEnumSqlAnalyzer
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool RequiresNormalization<T>(string sql, CommandType commandType)
    {
        if (commandType != CommandType.Text || string.IsNullOrWhiteSpace(sql))
            return false;

        var enumMap = ForgeORM.Core.Performance.ForgeRawEnumParameterMap<T>.Map;
        if (enumMap.Count == 0)
            return false;

        // Do this once during plan creation, not once per execution.
        // QueryById should not pay enum/raw-SQL rewriting cost when the SQL does not reference enum columns.
        foreach (var item in enumMap)
        {
            if (ContainsIdentifier(sql, item.Key))
                return true;
        }

        return false;
    }

    private static bool ContainsIdentifier(string sql, string identifier)
    {
        var index = 0;
        while ((index = sql.IndexOf(identifier, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var before = index == 0 ? '\0' : sql[index - 1];
            var afterIndex = index + identifier.Length;
            var after = afterIndex >= sql.Length ? '\0' : sql[afterIndex];

            if (!IsIdentifierChar(before) && !IsIdentifierChar(after))
                return true;

            index = afterIndex;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIdentifierChar(char ch)
        => char.IsLetterOrDigit(ch) || ch == '_' || ch == '@' || ch == ':';
}

internal readonly record struct ForgeCompiledExecutionPlanKey(
    string Provider,
    Type ResultType,
    Type? ParameterType,
    CommandType CommandType,
    CommandBehavior Behavior,
    ulong SqlFingerprint);

/// <summary>
/// Compiles reusable parameter layout/binder delegates. Command instances are still new per execution, but reflection and parameter discovery are not.
/// </summary>
internal static class ForgeParameterBinderCompiler
{
    private static readonly ConcurrentDictionary<ForgeParameterBinderKey, Action<DbCommand, object?>> Cache = new();

    public static Action<DbCommand, object?> Compile(Type? parameterType, string sql, CommandType commandType, ulong sqlHash)
    {
        var key = new ForgeParameterBinderKey(parameterType, commandType, sqlHash);
        return Cache.GetOrAdd(key, _ => Build(parameterType, sql, commandType));
    }

    private static Action<DbCommand, object?> Build(Type? parameterType, string sql, CommandType commandType)
    {
        var sqlNames = commandType == CommandType.Text ? ExtractParameterNames(sql, commandType) : Array.Empty<string>();

        if (parameterType is null)
            return static (_, _) => { };

        if (ForgeSourceGeneratedRegistry.CompilationMode != ForgeOrmCompilationMode.RuntimeEmit
            && ForgeSourceGeneratedRegistry.TryGetProvider(parameterType, out var provider))
        {
            var typedBinder = TryCreateTypedGeneratedBinder(provider, parameterType);
            if (typedBinder is not null)
                return typedBinder;

            if (provider.TryGetBinder(parameterType, out var generated) && generated is not null)
            {
                return (command, value) =>
                {
                    if (value is not null)
                        generated(command, value);
                };
            }
        }

        if (IsScalar(parameterType))
            return (command, value) => BindScalar(command, value, sqlNames);

        if (typeof(System.Collections.IDictionary).IsAssignableFrom(parameterType))
            return BindDictionary;

        var props = parameterType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.GetIndexParameters().Length == 0 && p.GetMethod is not null)
            .Select(p => new ForgeParameterProperty(p.Name, CompileGetter(parameterType, p), p.PropertyType, p))
            .ToArray();

        return (command, value) =>
        {
            if (value is null) return;
            for (var i = 0; i < props.Length; i++)
            {
                var p = props[i];
                Add(command, p.Name, p.Getter(value), p.PropertyType, p.Property);
            }

            // Safety: bind scalar-looking SQL names if property casing/prefix did not match.
            if (sqlNames.Length > 0)
            {
                for (var i = 0; i < sqlNames.Length; i++)
                {
                    if (HasParameter(command, sqlNames[i])) continue;
                    for (var x = 0; x < props.Length; x++)
                    {
                        if (!string.Equals(props[x].Name, sqlNames[i], StringComparison.OrdinalIgnoreCase)) continue;
                        Add(command, sqlNames[i], props[x].Getter(value), props[x].PropertyType, props[x].Property);
                        break;
                    }
                }
            }
        };
    }

    private static Action<DbCommand, object?>? TryCreateTypedGeneratedBinder(IForgeSourceGeneratedAccessorProvider provider, Type parameterType)
    {
        var method = typeof(ForgeParameterBinderCompiler)
            .GetMethod(nameof(CreateTypedGeneratedBinder), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(parameterType);

        return (Action<DbCommand, object?>?)method.Invoke(null, new object[] { provider });
    }

    private static Action<DbCommand, object?>? CreateTypedGeneratedBinder<T>(IForgeSourceGeneratedAccessorProvider provider)
    {
        return provider.TryGetTypedBinder<T>(out var binder) && binder is not null
            ? (command, value) =>
            {
                if (value is T typed) binder.Bind(command, typed);
            }
            : null;
    }

    private static Func<object, object?> CompileGetter(Type declaringType, PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var cast = Expression.Convert(instance, declaringType);
        var access = Expression.Property(cast, property);
        var box = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<object, object?>>(box, instance).Compile();
    }

    private static void BindDictionary(DbCommand command, object? value)
    {
        if (value is null) return;
        if (value is IReadOnlyDictionary<string, object?> ro)
        {
            foreach (var item in ro)
                Add(command, item.Key, item.Value, item.Value?.GetType());
            return;
        }
        if (value is System.Collections.IDictionary dict)
        {
            foreach (System.Collections.DictionaryEntry item in dict)
            {
                if (item.Key is null) continue;
                var name = Convert.ToString(item.Key, System.Globalization.CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(name)) Add(command, name!, item.Value, item.Value?.GetType());
            }
        }
    }

    private static void BindScalar(DbCommand command, object? value, string[] sqlNames)
    {
        if (value is null) return;
        if (sqlNames.Length == 0)
        {
            Add(command, "Value", value, value.GetType());
            return;
        }
        for (var i = 0; i < sqlNames.Length; i++)
            Add(command, sqlNames[i], value, value.GetType());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(DbCommand command, string name, object? value, Type? valueType, PropertyInfo? property = null)
    {
        // SQL Server and most ADO.NET providers cannot bind List<int>/T[] as a single scalar value.
        // Raw SQL and split-query paths commonly render: WHERE Id IN @Ids.
        // Expand those enumerable parameters into provider-safe scalar parameters before execution:
        // WHERE Id IN (@Ids0, @Ids1, ...). Strings and byte[] remain scalar.
        if (IsEnumerableParameter(value))
        {
            ExpandEnumerableParameter(command, name, (IEnumerable)value!);
            return;
        }

        var parameterName = NormalizeParameterName(name);
        var parameter = FindParameter(command, parameterName);
        if (parameter is null)
        {
            parameter = command.CreateParameter();
            parameter.ParameterName = parameterName;
            command.Parameters.Add(parameter);
        }
        parameter.Value = NormalizeParameterValue(value, valueType, property);
    }

    private static bool IsEnumerableParameter(object? value)
    {
        if (value is null)
            return false;

        if (value is string or byte[])
            return false;

        return value is IEnumerable;
    }

    private static void ExpandEnumerableParameter(DbCommand command, string name, IEnumerable values)
    {
        var cleanName = name.TrimStart('@', ':');

        // Important: one logical collection parameter can be seen twice by the binder
        // (once from the object property pass and once from the SQL-name safety pass).
        // Remove both the original parameter and any previously expanded @Ids0/@Ids1 family
        // before adding the new scalar parameters, otherwise SqlClient throws:
        // "The variable name '@Ids0' has already been declared."
        RemoveParameterFamily(command, cleanName);

        var parameterNames = new List<string>(8);
        var index = 0;

        foreach (var item in values)
        {
            string expandedParameterName;
            do
            {
                var expandedName = cleanName + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                expandedParameterName = NormalizeParameterName(expandedName);
                index++;
            }
            while (FindParameter(command, expandedParameterName) is not null);

            var parameter = command.CreateParameter();
            parameter.ParameterName = expandedParameterName;
            parameter.Value = NormalizeParameterValue(item, item?.GetType());
            command.Parameters.Add(parameter);

            parameterNames.Add(expandedParameterName);
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

    private static bool IsExpandedParameterName(string current, string logicalName)
    {
        if (!current.StartsWith(logicalName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (current.Length == logicalName.Length)
            return true;

        var suffixStart = current[logicalName.Length];
        return char.IsDigit(suffixStart) || suffixStart == '_';
    }

    private static string ReplaceParameterToken(string sql, string parameterName, string replacement)
    {
        if (string.IsNullOrEmpty(sql))
            return sql;

        var atName = "@" + parameterName;
        var colonName = ":" + parameterName;

        return ReplaceToken(ReplaceToken(sql, atName, replacement), colonName, replacement);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object NormalizeParameterValue(object? value, Type? declaredType, PropertyInfo? property = null)
    {
        if (value is null)
            return DBNull.Value;

        var type = Nullable.GetUnderlyingType(declaredType ?? value.GetType()) ?? (declaredType ?? value.GetType());
        if (type.IsEnum)
        {
            // Enterprise default: bind enums as their underlying numeric value, same as EF Core.
            // Reading remains storage-agnostic and accepts both int and string database values.
            // String enum binding is only an explicit opt-in; it is never required for materialization.
            if (IsStringEnumStorage(property))
                return value ?? DBNull.Value;

            return Convert.ChangeType(value, Enum.GetUnderlyingType(type), System.Globalization.CultureInfo.InvariantCulture) ?? DBNull.Value;
        }

        return value;
    }

    private static bool IsStringEnumStorage(PropertyInfo? property)
    {
        if (property is null)
            return false;

        var attr = property.GetCustomAttributes(inherit: true)
            .FirstOrDefault(a => a.GetType().Name is "ForgeEnumStorageAttribute" or "EnumStorageAttribute");

        if (attr is null)
            return false;

        var storageProperty = attr.GetType().GetProperty("Storage");
        var storage = storageProperty?.GetValue(attr);
        return storage?.ToString()?.Equals("String", StringComparison.OrdinalIgnoreCase) == true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string NormalizeParameterName(string name)
        => name.Length > 0 && (name[0] == '@' || name[0] == ':') ? name : "@" + name;

    private static DbParameter? FindParameter(DbCommand command, string parameterName)
    {
        var normalized = parameterName.TrimStart('@', ':');
        for (var i = 0; i < command.Parameters.Count; i++)
        {
            if (command.Parameters[i] is not DbParameter p) continue;
            if (string.Equals(p.ParameterName.TrimStart('@', ':'), normalized, StringComparison.OrdinalIgnoreCase))
                return p;
        }
        return null;
    }

    private static bool HasParameter(DbCommand command, string name)
    {
        var normalized = name.TrimStart('@', ':');
        for (var i = 0; i < command.Parameters.Count; i++)
        {
            if (command.Parameters[i] is not DbParameter p) continue;
            var current = p.ParameterName.TrimStart('@', ':');
            if (string.Equals(current, normalized, StringComparison.OrdinalIgnoreCase) || IsExpandedParameterName(current, normalized))
                return true;
        }
        return false;
    }

    internal static string[] ExtractParameterNames(string sql, CommandType commandType = CommandType.Text)
    {
        if (commandType != CommandType.Text) return Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(sql)) return Array.Empty<string>();
        var names = new List<string>(4);
        for (var i = 0; i < sql.Length - 1; i++)
        {
            var marker = sql[i];
            if (marker is not '@' and not ':') continue;
            if (marker == '@' && i > 0 && sql[i - 1] == '@') continue;
            var start = i + 1;
            if (start >= sql.Length || !(char.IsLetter(sql[start]) || sql[start] == '_')) continue;
            var end = start + 1;
            while (end < sql.Length && (char.IsLetterOrDigit(sql[end]) || sql[end] == '_')) end++;
            var name = sql[start..end];
            var exists = false;
            for (var n = 0; n < names.Count; n++)
            {
                if (!string.Equals(names[n], name, StringComparison.OrdinalIgnoreCase)) continue;
                exists = true; break;
            }
            if (!exists) names.Add(name);
            i = end - 1;
        }
        return names.Count == 0 ? Array.Empty<string>() : names.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly)
            || type == typeof(TimeSpan) || type == typeof(byte[]);
    }
}

internal readonly record struct ForgeParameterBinderKey(Type? ParameterType, CommandType CommandType, ulong SqlFingerprint);
internal readonly record struct ForgeParameterProperty(string Name, Func<object, object?> Getter, Type PropertyType, PropertyInfo Property);
