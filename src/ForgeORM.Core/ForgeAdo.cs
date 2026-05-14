using System.Collections;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace ForgeORM.Core;

public static class ForgeAdo
{
    public static IReadOnlyList<T> Query<T>(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
        => QueryAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();
    public static async Task<IReadOnlyList<T>> QueryAsync<T>(
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

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<T>();

        while (await reader.ReadAsync(cancellationToken))
            rows.Add(ForgeMaterializer.Map<T>(reader));

        return rows;
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
        var rows = await QueryAsync<T>(
            connection,
            sql,
            parameters,
            transaction,
            commandType,
            timeoutSeconds,
            cancellationToken);

        return rows.FirstOrDefault();
    }
    public static int Execute(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
      => ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();
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
    public static T? ExecuteScalar<T>(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)
      => ExecuteScalarAsync<T>(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult();

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

        if (parameters is IReadOnlyDictionary<string, object?> dictionary)
        {
            foreach (var item in dictionary)
                BindSingleOrEnumerable(command, item.Key, item.Value, item.Value?.GetType());

            return;
        }

        foreach (var prop in parameters.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead))
        {
            var value = prop.GetValue(parameters);
            BindSingleOrEnumerable(command, prop.Name, value, prop.PropertyType);
        }
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
        parameter.Value = value ?? DBNull.Value;

        command.Parameters.Add(parameter);
    }

    private static bool IsEnumerableParameter(object? value)
    {
        if (value is null)
            return false;

        if (value is string or byte[])
            return false;

        return value is IEnumerable;
    }


}