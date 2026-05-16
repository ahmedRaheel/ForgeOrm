using System.Collections;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace ForgeORM.Core;

public static class ForgeAdo
{
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
        await using var command = CreateCommand(connection, sql, parameters, transaction, commandType, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<T>();

        while (await reader.ReadAsync(cancellationToken))
            rows.Add(ForgeMaterializer.Map<T>(reader));

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

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var rows = new List<IDictionary<string, object?>>();

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