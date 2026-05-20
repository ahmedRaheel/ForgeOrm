using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using ForgeORM.Abstractions;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

/// <summary>
/// SQL Server provider-direct hot path. This intentionally avoids DbCommand/DbDataReader
/// for the highest-frequency operations so the JIT can see SqlCommand/SqlDataReader and
/// the pipeline avoids generic command creation, dictionary planning, and list allocation
/// for single-row reads.
/// </summary>
internal static class ForgeSqlServerProviderDirectHotPath
{
    private static readonly ConcurrentDictionary<Type, SqlServerEntityPlan> GetByIdPlans = new();
    private static readonly ConcurrentDictionary<SqlQueryPlanKey, SqlQueryPlan> QueryPlans = new();
    private static readonly ConcurrentDictionary<string, string[]> SqlParameterTokenCache = new(StringComparer.Ordinal);

    public static bool CanUse(IForgeDatabaseProvider provider)
        => string.Equals(provider.ProviderName, "SqlServer", StringComparison.OrdinalIgnoreCase);

    public static T? GetById<T>(string connectionString, ForgeEntityMetadata metadata, object id)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = CreateGetByIdCommand(connection, metadata, id, null, null);
        using var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        return reader.Read() ? materializer(reader) : default;
    }

    public static async Task<T?> GetByIdAsync<T>(string connectionString, ForgeEntityMetadata metadata, object id, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = CreateGetByIdCommand(connection, metadata, id, null, null);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? materializer(reader) : default;
    }

    public static async Task<T?> QueryFirstOrDefaultAsync<T>(string connectionString, string sql, object? parameters, int? timeoutSeconds, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = CreateTextCommand(connection, sql, parameters, null, timeoutSeconds);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? materializer(reader) : default;
    }

    public static T? QueryFirstOrDefault<T>(string connectionString, string sql, object? parameters, int? timeoutSeconds)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = CreateTextCommand(connection, sql, parameters, null, timeoutSeconds);
        using var reader = command.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        return reader.Read() ? materializer(reader) : default;
    }

    public static async Task<IReadOnlyList<T>> QueryAsync<T>(string connectionString, string sql, object? parameters, int? timeoutSeconds, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = CreateTextCommand(connection, sql, parameters, null, timeoutSeconds);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        var rows = new List<T>(EstimateCapacity(sql));
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            rows.Add(materializer(reader));
        return rows;
    }

    public static IReadOnlyList<T> Query<T>(string connectionString, string sql, object? parameters, int? timeoutSeconds)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();
        using var command = CreateTextCommand(connection, sql, parameters, null, timeoutSeconds);
        using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        var rows = new List<T>(EstimateCapacity(sql));
        while (reader.Read())
            rows.Add(materializer(reader));
        return rows;
    }

    public static async Task<IReadOnlyList<Dictionary<string, object?>>> QueryDictionaryAsync(string connectionString, string sql, object? parameters, int? timeoutSeconds, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = CreateTextCommand(connection, sql, parameters, null, timeoutSeconds);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
        var rows = new List<Dictionary<string, object?>>(EstimateCapacity(sql));
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }

    public static async IAsyncEnumerable<T> StreamAsync<T>(string connectionString, string sql, object? parameters, int? timeoutSeconds, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = CreateTextCommand(connection, sql, parameters, null, timeoutSeconds);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return materializer(reader);
    }

    public static SqlCommand CreateGetByIdCommand(SqlConnection connection, ForgeEntityMetadata metadata, object id, SqlTransaction? transaction, int? timeoutSeconds)
    {
        var plan = GetByIdPlans.GetOrAdd(metadata.EntityType, _ => BuildGetByIdPlan(metadata));
        var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;
        if (transaction is not null)
            command.Transaction = transaction;
        if (timeoutSeconds.HasValue)
            command.CommandTimeout = timeoutSeconds.Value;
        AddTypedParameter(command, plan.ParameterName, id, plan.KeyType);
        EnsureScalarSqlParametersBound(command, id, plan.KeyType);
        return command;
    }

    public static SqlCommand CreateTextCommand(SqlConnection connection, string sql, object? parameters, SqlTransaction? transaction, int? timeoutSeconds)
    {
        var key = new SqlQueryPlanKey(sql, parameters?.GetType());
        var plan = QueryPlans.GetOrAdd(key, static k => new SqlQueryPlan(k.Sql, SqlParameterTokenCache.GetOrAdd(k.Sql, ExtractParameterNames)));
        var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;
        if (transaction is not null)
            command.Transaction = transaction;
        if (timeoutSeconds.HasValue)
            command.CommandTimeout = timeoutSeconds.Value;
        BindParameters(command, plan.ParameterNames, parameters);
        if (parameters is not null && IsScalar(parameters.GetType()))
            EnsureScalarSqlParametersBound(command, parameters, parameters.GetType());
        EnsureReferencedSqlParametersAreBound(command, plan.ParameterNames, parameters);

        // Enumerable expansion can rewrite IN @Ids into IN (@Ids0, @Ids1, ...).
        // Validate against the final command text, not the original plan tokens.
        var finalParameterNames = SqlParameterTokenCache.GetOrAdd(command.CommandText, ExtractParameterNames);
        ValidateNoUnboundSqlParameters(command, finalParameterNames);
        return command;
    }

    private static SqlServerEntityPlan BuildGetByIdPlan(ForgeEntityMetadata metadata)
    {
        var key = metadata.Properties.FirstOrDefault(x => string.Equals(x.ColumnName, metadata.KeyColumn, StringComparison.OrdinalIgnoreCase) || x.IsKey);
        var parameterName = "@" + (key?.PropertyName ?? metadata.KeyColumn).TrimStart('@', ':');
        return new SqlServerEntityPlan(
            $"SELECT * FROM {metadata.TableName} WHERE {metadata.KeyColumn} = {parameterName}",
            parameterName,
            key?.PropertyType ?? typeof(object));
    }

    private static void BindParameters(SqlCommand command, string[] parameterNames, object? parameters)
    {
        if (parameters is null)
            return;

        if (IsScalar(parameters.GetType()))
        {
            if (parameterNames.Length == 0)
                AddTypedParameter(command, "@Value", parameters, parameters.GetType());
            else
                foreach (var name in parameterNames)
                    AddTypedParameter(command, name, parameters, parameters.GetType());
            return;
        }

        if (parameters is IReadOnlyDictionary<string, object?> dictionary)
        {
            foreach (var name in parameterNames)
            {
                var key = name.TrimStart('@');
                if (dictionary.TryGetValue(key, out var value) || dictionary.TryGetValue(name, out value))
                    AddTypedParameterOrEnumerable(command, name, value, value?.GetType() ?? typeof(object));
            }
            return;
        }

        if (parameters is IDictionary nonGenericDictionary)
        {
            foreach (var name in parameterNames)
            {
                var key = name.TrimStart('@');
                object? value = null;
                if (nonGenericDictionary.Contains(key))
                    value = nonGenericDictionary[key];
                else if (nonGenericDictionary.Contains(name))
                    value = nonGenericDictionary[name];
                else
                    continue;

                AddTypedParameterOrEnumerable(command, name, value, value?.GetType() ?? typeof(object));
            }
            return;
        }

        var binder = ForgeSqlServerParameterBinderCache.GetOrAdd(parameters.GetType(), parameterNames);
        binder(command, parameters);
    }

    public static void AddTypedParameterOrEnumerable(SqlCommand command, string parameterName, object? value, Type declaredType)
    {
        if (IsEnumerableParameter(value))
        {
            ExpandEnumerableParameter(command, parameterName, (IEnumerable)value!);
            return;
        }

        AddTypedParameter(command, parameterName, value, declaredType);
    }

    internal static void AddTypedParameter(SqlCommand command, string parameterName, object? value, Type declaredType)
    {
        var runtimeType = value?.GetType() ?? declaredType;
        var actualType = Nullable.GetUnderlyingType(runtimeType) ?? runtimeType;
        if (actualType == typeof(object)) actualType = typeof(string);
        if (!parameterName.StartsWith('@')) parameterName = "@" + parameterName;

        SqlParameter parameter;
        if (actualType == typeof(int)) parameter = command.Parameters.Add(parameterName, SqlDbType.Int);
        else if (actualType == typeof(long)) parameter = command.Parameters.Add(parameterName, SqlDbType.BigInt);
        else if (actualType == typeof(short)) parameter = command.Parameters.Add(parameterName, SqlDbType.SmallInt);
        else if (actualType == typeof(byte)) parameter = command.Parameters.Add(parameterName, SqlDbType.TinyInt);
        else if (actualType == typeof(Guid)) parameter = command.Parameters.Add(parameterName, SqlDbType.UniqueIdentifier);
        else if (actualType == typeof(decimal)) parameter = command.Parameters.Add(parameterName, SqlDbType.Decimal);
        else if (actualType == typeof(double)) parameter = command.Parameters.Add(parameterName, SqlDbType.Float);
        else if (actualType == typeof(float)) parameter = command.Parameters.Add(parameterName, SqlDbType.Real);
        else if (actualType == typeof(bool)) parameter = command.Parameters.Add(parameterName, SqlDbType.Bit);
        else if (actualType == typeof(DateTime)) parameter = command.Parameters.Add(parameterName, SqlDbType.DateTime2);
        else if (actualType == typeof(DateTimeOffset)) parameter = command.Parameters.Add(parameterName, SqlDbType.DateTimeOffset);
        else if (actualType == typeof(DateOnly)) parameter = command.Parameters.Add(parameterName, SqlDbType.Date);
        else if (actualType == typeof(TimeOnly) || actualType == typeof(TimeSpan)) parameter = command.Parameters.Add(parameterName, SqlDbType.Time);
        else if (actualType == typeof(byte[])) parameter = command.Parameters.Add(parameterName, SqlDbType.VarBinary);
        else parameter = command.Parameters.Add(parameterName, SqlDbType.NVarChar);

        if (actualType.IsEnum)
            parameter.Value = value ?? DBNull.Value;
        else if (value is DateOnly d)
            parameter.Value = d.ToDateTime(TimeOnly.MinValue);
        else if (value is TimeOnly t)
            parameter.Value = t.ToTimeSpan();
        else
            parameter.Value = value ?? DBNull.Value;
    }


    private static void EnsureReferencedSqlParametersAreBound(SqlCommand command, string[] parameterNames, object? parameters)
    {
        if (parameters is null || parameterNames.Length == 0)
            return;

        if (parameters is IReadOnlyDictionary<string, object?> dictionary)
        {
            foreach (var name in parameterNames)
            {
                if (HasParameter(command, name))
                    continue;
                var key = name.TrimStart('@', ':');
                if (dictionary.TryGetValue(key, out var value) || dictionary.TryGetValue(name, out value))
                    AddTypedParameterOrEnumerable(command, name, value, value?.GetType() ?? typeof(object));
            }
            return;
        }

        if (parameters is IDictionary nonGenericDictionary)
        {
            foreach (var name in parameterNames)
            {
                if (HasParameter(command, name))
                    continue;
                var key = name.TrimStart('@', ':');
                object? value = null;
                var found = false;
                if (nonGenericDictionary.Contains(key)) { value = nonGenericDictionary[key]; found = true; }
                else if (nonGenericDictionary.Contains(name)) { value = nonGenericDictionary[name]; found = true; }
                if (found)
                    AddTypedParameterOrEnumerable(command, name, value, value?.GetType() ?? typeof(object));
            }
            return;
        }

        if (IsScalar(parameters.GetType()))
        {
            EnsureScalarSqlParametersBound(command, parameters, parameters.GetType());
        }
    }

    private static void ValidateNoUnboundSqlParameters(SqlCommand command, string[] parameterNames)
    {
        foreach (var name in parameterNames)
        {
            if (!HasParameter(command, name))
            {
                throw new InvalidOperationException(
                    $"ForgeORM SQL Server direct command is missing SQL parameter '{name}'. " +
                    "Pass a scalar value for single-token SQL, or pass an anonymous object/dictionary with a matching property/key.");
            }
        }
    }

    private static void EnsureScalarSqlParametersBound(SqlCommand command, object? value, Type declaredType)
    {
        if (string.IsNullOrWhiteSpace(command.CommandText))
            return;

        foreach (var name in SqlParameterTokenCache.GetOrAdd(command.CommandText, ExtractParameterNames))
        {
            if (!HasParameter(command, name))
                AddTypedParameter(command, name, value, declaredType);
        }
    }

    private static bool HasParameter(SqlCommand command, string name)
    {
        if (!name.StartsWith("@", StringComparison.Ordinal))
            name = "@" + name;

        foreach (SqlParameter parameter in command.Parameters)
        {
            if (string.Equals(parameter.ParameterName, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }


    private static bool IsEnumerableParameter(object? value)
    {
        if (value is null || value is string || value is byte[])
            return false;

        return value is IEnumerable;
    }

    private static void ExpandEnumerableParameter(SqlCommand command, string parameterName, IEnumerable values)
    {
        var cleanName = parameterName.TrimStart('@', ':');
        var parameterNames = new List<string>();
        var index = 0;

        foreach (var value in values)
        {
            var expandedName = "@" + cleanName + index++;
            parameterNames.Add(expandedName);
            AddTypedParameter(command, expandedName, value, value?.GetType() ?? typeof(object));
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

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan) || type == typeof(byte[]);
    }

    private static string[] ExtractParameterNames(string sql)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(sql, @"(?<!@)@[A-Za-z_][A-Za-z0-9_]*");
        if (matches.Count == 0) return Array.Empty<string>();
        return matches.Select(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static int EstimateCapacity(string sql)
        => sql.Contains("TOP 1", StringComparison.OrdinalIgnoreCase) || sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase) ? 1 : 32;

    private sealed record SqlServerEntityPlan(string Sql, string ParameterName, Type KeyType);
    private readonly record struct SqlQueryPlanKey(string Sql, Type? ParameterType);
    private sealed record SqlQueryPlan(string Sql, string[] ParameterNames);
}
