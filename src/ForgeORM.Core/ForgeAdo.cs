using System.Collections;
using System.Data;
using System.Dynamic;
using System.Data.Common;
using System.Reflection;

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
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
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

        if (targetType == typeof(string) || targetType.IsPrimitive || targetType.IsEnum || targetType == typeof(decimal) || targetType == typeof(DateTime) || targetType == typeof(Guid))
            return ConvertValue<T>(reader.IsDBNull(0) ? null : reader.GetValue(0))!;

        var instance = Activator.CreateInstance<T>();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite).ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!props.TryGetValue(reader.GetName(i), out var prop) || reader.IsDBNull(i)) continue;
            var value = reader.GetValue(i);
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            prop.SetValue(instance, propType.IsEnum ? Enum.ToObject(propType, value) : Convert.ChangeType(value, propType));
        }
        return instance;
    }

    private static T? ConvertValue<T>(object? value)
    {
        if (value is null || value is DBNull) return default;
        var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (type.IsEnum) return (T)Enum.ToObject(type, value);
        if (value is T typed) return typed;
        return (T)Convert.ChangeType(value, type);
    }

    private static bool IsBatch(object? parameters)
        => parameters is not null and IEnumerable and not string and not byte[]
           && parameters is not IDictionary
           && parameters is not IEnumerable<KeyValuePair<string, object?>>;
}
