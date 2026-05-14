using System.Collections;
using System.Data;
using System.Dynamic;
using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal static class ForgeAdo
{
    public static IReadOnlyList<T> Query<T>(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => QueryAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();

    public static async Task<IReadOnlyList<T>> QueryAsync<T>(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(Map<T>(reader));
        return rows;
    }

    public static int Execute(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();

    public static async Task<int> ExecuteAsync(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        var total = 0;
        if (IsBatch(parameters))
        {
            foreach (var row in (IEnumerable)parameters!)
            {
                await using var command = CreateCommand(connection, sql, row, transaction, commandType, timeoutSeconds);
                total += await command.ExecuteNonQueryAsync(cancellationToken);
            }
            return total;
        }

        await using var single = CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);
        return await single.ExecuteNonQueryAsync(cancellationToken);
    }

    public static T? ExecuteScalar<T>(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => ExecuteScalarAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();

    public static async Task<T?> ExecuteScalarAsync<T>(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return ConvertValue<T>(value);
    }

    public static DbCommand CreateCommand(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = commandType;
        if (timeoutSeconds.HasValue) command.CommandTimeout = timeoutSeconds.Value;
        if (transaction is not null) command.Transaction = transaction;
        BindParameters(command, parameters);
        return command;
    }

    private static void BindParameters(DbCommand command, object? parameters)
    {
        if (parameters is null) return;
        if (parameters is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach (var pair in pairs) AddParameter(command, pair.Key, pair.Value);
            return;
        }
        if (parameters is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary) AddParameter(command, entry.Key.ToString()!, entry.Value);
            return;
        }
        foreach (var prop in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead))
        {
            var value = prop.GetValue(parameters);
            if (value is string || value is null || value is not IEnumerable enumerable)
            {
                AddParameter(command, prop.Name, value);
                continue;
            }

            var values = enumerable.Cast<object?>().ToList();
            if (values.Count == 0)
            {
                command.CommandText = command.CommandText.Replace("@" + prop.Name, "(NULL)");
                continue;
            }
            var names = new List<string>();
            for (var i = 0; i < values.Count; i++)
            {
                var name = prop.Name + i;
                names.Add("@" + name);
                AddParameter(command, name, values[i]);
            }
            command.CommandText = command.CommandText.Replace("@" + prop.Name, string.Join(", ", names));
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        if (!name.StartsWith('@')) name = "@" + name;
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = NormalizeParameterValue(value) ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static object? NormalizeParameterValue(object? value)
    {
        if (value is null || value is DBNull) return value;

        var valueType = value.GetType();
        var enumType = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (enumType.IsEnum)
            return value.ToString(); // default ForgeORM behavior: enums are stored as readable strings

        return value;
    }

    public static T Map<T>(DbDataReader reader)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (targetType == typeof(object) || targetType == typeof(ExpandoObject))
        {
            IDictionary<string, object?> row = new ExpandoObject();
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            return (T)row;
        }

        if (IsSimple(targetType))
            return ConvertValue<T>(reader.IsDBNull(0) ? null : reader.GetValue(0))!;

        var parameterless = targetType.GetConstructor(Type.EmptyTypes);
        return parameterless is not null
            ? MapByProperties<T>(reader)
            : MapByConstructor<T>(reader);
    }

    private static T MapByProperties<T>(DbDataReader reader)
    {
        var instance = Activator.CreateInstance<T>();
        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => ResolveColumnName(p), StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!props.TryGetValue(reader.GetName(i), out var prop) || reader.IsDBNull(i)) continue;
            prop.SetValue(instance, ConvertTo(reader.GetValue(i), prop.PropertyType));
        }
        return instance;
    }

    private static T MapByConstructor<T>(DbDataReader reader)
    {
        var type = typeof(T);
        var columns = GetColumnLookup(reader);
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToList();

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            var values = new object?[parameters.Length];
            var canUse = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (!columns.TryGetValue(parameter.Name ?? string.Empty, out var ordinal))
                {
                    if (!parameter.HasDefaultValue && IsRequiredConstructorParameter(parameter.ParameterType))
                    {
                        canUse = false;
                        break;
                    }
                    values[i] = parameter.HasDefaultValue ? parameter.DefaultValue : GetDefault(parameter.ParameterType);
                    continue;
                }

                values[i] = reader.IsDBNull(ordinal)
                    ? GetDefault(parameter.ParameterType)
                    : ConvertTo(reader.GetValue(ordinal), parameter.ParameterType);
            }

            if (canUse)
                return (T)ctor.Invoke(values);
        }

        throw new InvalidOperationException($"ForgeORM could not map result to {type.FullName}. Add a parameterless constructor or make constructor parameter names match selected column aliases.");
    }

    private static Dictionary<string, int> GetColumnLookup(DbDataReader reader)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            columns[reader.GetName(i)] = i;
        return columns;
    }

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type == typeof(string)
            || type.IsPrimitive
            || type.IsEnum
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(Guid)
            || type == typeof(TimeSpan);
    }

    private static object? ConvertTo(object? value, Type targetType)
    {
        if (value is null || value is DBNull) return GetDefault(targetType);
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type.IsEnum)
        {
            if (value is string text)
                return Enum.Parse(type, text, ignoreCase: true);
            return Enum.ToObject(type, value);
        }
        if (type == typeof(Guid)) return value is Guid g ? g : Guid.Parse(value.ToString()!);
        if (type == typeof(DateTimeOffset)) return value is DateTimeOffset dto ? dto : new DateTimeOffset(Convert.ToDateTime(value));
        if (type == typeof(TimeSpan)) return value is TimeSpan ts ? ts : TimeSpan.Parse(value.ToString()!);
        if (type == typeof(string)) return value.ToString();
        if (type.IsAssignableFrom(value.GetType())) return value;
        return Convert.ChangeType(value, type);
    }

    private static object? GetDefault(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null) return null;
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static bool IsRequiredConstructorParameter(Type type)
        => Nullable.GetUnderlyingType(type) is null && type.IsValueType;

    private static string ResolveColumnName(PropertyInfo property)
    {
        var attr = property.GetCustomAttributes(false)
            .FirstOrDefault(x => x.GetType().Name == "ForgeColumnAttribute");
        var name = attr?.GetType().GetProperty("Name")?.GetValue(attr)?.ToString();
        return string.IsNullOrWhiteSpace(name) ? property.Name : name!;
    }

    private static T? ConvertValue<T>(object? value)
    {
        if (value is null || value is DBNull) return default;
        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (type.IsEnum)
        {
            if (value is string text)
                return (T)Enum.Parse(type, text, ignoreCase: true);
            return (T)Enum.ToObject(type, value);
        }
        if (value is T typed) return typed;
        return (T)Convert.ChangeType(value, type);
    }

    private static bool IsBatch(object? parameters)
        => parameters is not null and IEnumerable and not string and not byte[]
           && parameters is not IDictionary
           && parameters is not IEnumerable<KeyValuePair<string, object?>>;
}
