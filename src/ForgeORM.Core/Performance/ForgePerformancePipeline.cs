using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Central high-performance ADO.NET pipeline. It no longer delegates query execution to ForgeAdo;
/// it owns plan lookup, parameter binding, command execution and source-generated/MSIL materialization.
/// </summary>
public static class ForgePerformancePipeline
{
    public static async ValueTask<IReadOnlyList<T>> QueryAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var plan = ForgeCompiledExecutionPlanCache.GetOrAdd<T>(connection, sql, parameters, commandType, CommandBehavior.SequentialAccess);
        await using var command = CreateCommand(connection, plan, parameters, transaction, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await ExecuteReaderListAsync<T>(command, plan, ForgeCommandOperation.Query, EstimateCapacity(sql), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Lower-allocation typed parameter overload. Use this from new public APIs when the parameter type is known.
    /// It avoids boxing the parameter container before it reaches the binder cache.
    /// </summary>
    public static async ValueTask<IReadOnlyList<T>> QueryAsync<T, TParameters>(
        DbConnection connection,
        string sql,
        TParameters parameters,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var plan = ForgeCompiledExecutionPlanCache.GetOrAdd<T>(connection, sql, parameters, commandType, CommandBehavior.SequentialAccess);
        await using var command = CreateCommand(connection, plan, parameters, transaction, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await ExecuteReaderListAsync<T>(command, plan, ForgeCommandOperation.Query, EstimateCapacity(sql), cancellationToken)
            .ConfigureAwait(false);
    }

    public static async IAsyncEnumerable<T> StreamAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var plan = ForgeCompiledExecutionPlanCache.GetOrAdd<T>(connection, sql, parameters, commandType, CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection);
        await using var command = CreateCommand(connection, plan, parameters, transaction, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var context = ForgeEnterpriseRuntime.CreateContext(command, ForgeCommandOperation.Stream, typeof(T), plan.ParameterType, plan.QueryFingerprint);
        var stopwatch = ForgeEnterpriseRuntime.IsEnabled ? Stopwatch.StartNew() : null;
        try
        {
            if (ForgeEnterpriseRuntime.IsEnabled)
                await ForgeEnterpriseRuntime.OnExecutingAsync(command, context, cancellationToken).ConfigureAwait(false);

            await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
            var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
            var rowCount = 0;
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rowCount++;
                yield return materializer(reader);
            }

            if (ForgeEnterpriseRuntime.IsEnabled)
                await ForgeEnterpriseRuntime.OnExecutedAsync(command, new ForgeCommandExecutionResult(context, stopwatch!.Elapsed, rowCount, null), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            stopwatch?.Stop();
        }
    }

    public static async ValueTask<T?> FirstOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var plan = ForgeCompiledExecutionPlanCache.GetOrAdd<T>(connection, sql, parameters, commandType, CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
        await using var command = CreateCommand(connection, plan, parameters, transaction, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await ExecuteReaderSingleAsync<T>(command, plan, ForgeCommandOperation.FirstOrDefault, requireSingle: false, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<T?> SingleOrDefaultAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var plan = ForgeCompiledExecutionPlanCache.GetOrAdd<T>(connection, sql, parameters, commandType, CommandBehavior.SequentialAccess);
        await using var command = CreateCommand(connection, plan, parameters, transaction, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await ExecuteReaderSingleAsync<T>(command, plan, ForgeCommandOperation.SingleOrDefault, requireSingle: true, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async ValueTask<int> ExecuteAsync(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var plan = ForgeCompiledExecutionPlanCache.GetOrAdd<int>(connection, sql, parameters, commandType, CommandBehavior.Default);
        await using var command = CreateCommand(connection, plan, parameters, transaction, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await ExecuteNonQueryWithEnterpriseHooksAsync(command, plan, cancellationToken).ConfigureAwait(false);
    }


    public static async ValueTask<T?> ExecuteScalarAsync<T>(
        DbConnection connection,
        string sql,
        object? parameters = null,
        DbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        var plan = ForgeCompiledExecutionPlanCache.GetOrAdd<T>(connection, sql, parameters, commandType, CommandBehavior.SingleResult);
        await using var command = CreateCommand(connection, plan, parameters, transaction, timeoutSeconds);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var value = await ExecuteScalarWithEnterpriseHooksAsync(command, plan, cancellationToken).ConfigureAwait(false);
        return ForgeScalarConverter.To<T>(value);
    }

    public static async ValueTask<ForgePagedResult<T>> PageAsync<T>(
        DbConnection connection,
        IForgeDatabaseProvider provider,
        ForgePageRequest request,
        CancellationToken cancellationToken = default)
    {
        var count = provider.BuildCount(request.Sql, request.Parameters);
        var total = await ExecuteScalarAsync<int>(connection, count.CommandText, count.Parameters, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var page = provider.BuildPage(request);
        var rows = await QueryAsync<T>(connection, page.CommandText, page.Parameters, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return new ForgePagedResult<T> { Items = rows, Page = request.Page, PageSize = request.PageSize, TotalRecords = total };
    }


    private static async ValueTask<IReadOnlyList<T>> ExecuteReaderListAsync<T>(
        DbCommand command,
        ForgeCompiledQueryPlan<T> plan,
        ForgeCommandOperation operation,
        int capacity,
        CancellationToken cancellationToken)
    {
        if (!ForgeEnterpriseRuntime.IsEnabled)
        {
            await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
            var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
            var rows = new List<T>(capacity);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                rows.Add(materializer(reader));
            return rows;
        }

        var context = ForgeEnterpriseRuntime.CreateContext(command, operation, typeof(T), plan.ParameterType, plan.QueryFingerprint);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await ForgeEnterpriseRuntime.OnExecutingAsync(command, context, cancellationToken).ConfigureAwait(false);
            await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
            var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
            var rows = new List<T>(capacity);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                rows.Add(materializer(reader));
            await ForgeEnterpriseRuntime.OnExecutedAsync(command, new ForgeCommandExecutionResult(context, stopwatch.Elapsed, rows.Count, null), cancellationToken).ConfigureAwait(false);
            return rows;
        }
        catch (Exception ex)
        {
            await ForgeEnterpriseRuntime.OnFailedAsync(command, new ForgeCommandExecutionResult(context, stopwatch.Elapsed, null, ex), cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private static async ValueTask<T?> ExecuteReaderSingleAsync<T>(
        DbCommand command,
        ForgeCompiledQueryPlan<T> plan,
        ForgeCommandOperation operation,
        bool requireSingle,
        CancellationToken cancellationToken)
    {
        if (!ForgeEnterpriseRuntime.IsEnabled)
        {
            await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return default;
            var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
            var first = materializer(reader);
            if (requireSingle && await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("Sequence contains more than one element.");
            return first;
        }

        var context = ForgeEnterpriseRuntime.CreateContext(command, operation, typeof(T), plan.ParameterType, plan.QueryFingerprint);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await ForgeEnterpriseRuntime.OnExecutingAsync(command, context, cancellationToken).ConfigureAwait(false);
            await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                await ForgeEnterpriseRuntime.OnExecutedAsync(command, new ForgeCommandExecutionResult(context, stopwatch.Elapsed, 0, null), cancellationToken).ConfigureAwait(false);
                return default;
            }
            var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
            var first = materializer(reader);
            if (requireSingle && await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("Sequence contains more than one element.");
            await ForgeEnterpriseRuntime.OnExecutedAsync(command, new ForgeCommandExecutionResult(context, stopwatch.Elapsed, 1, null), cancellationToken).ConfigureAwait(false);
            return first;
        }
        catch (Exception ex)
        {
            await ForgeEnterpriseRuntime.OnFailedAsync(command, new ForgeCommandExecutionResult(context, stopwatch.Elapsed, null, ex), cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private static async ValueTask<int> ExecuteNonQueryWithEnterpriseHooksAsync<T>(DbCommand command, ForgeCompiledQueryPlan<T> plan, CancellationToken cancellationToken)
    {
        if (!ForgeEnterpriseRuntime.IsEnabled)
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        var context = ForgeEnterpriseRuntime.CreateContext(command, ForgeCommandOperation.Execute, typeof(T), plan.ParameterType, plan.QueryFingerprint);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await ForgeEnterpriseRuntime.OnExecutingAsync(command, context, cancellationToken).ConfigureAwait(false);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            await ForgeEnterpriseRuntime.OnExecutedAsync(command, new ForgeCommandExecutionResult(context, stopwatch.Elapsed, affected, null), cancellationToken).ConfigureAwait(false);
            return affected;
        }
        catch (Exception ex)
        {
            await ForgeEnterpriseRuntime.OnFailedAsync(command, new ForgeCommandExecutionResult(context, stopwatch.Elapsed, null, ex), cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private static async ValueTask<object?> ExecuteScalarWithEnterpriseHooksAsync<T>(DbCommand command, ForgeCompiledQueryPlan<T> plan, CancellationToken cancellationToken)
    {
        if (!ForgeEnterpriseRuntime.IsEnabled)
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        var context = ForgeEnterpriseRuntime.CreateContext(command, ForgeCommandOperation.Scalar, typeof(T), plan.ParameterType, plan.QueryFingerprint);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await ForgeEnterpriseRuntime.OnExecutingAsync(command, context, cancellationToken).ConfigureAwait(false);
            var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            await ForgeEnterpriseRuntime.OnExecutedAsync(command, new ForgeCommandExecutionResult(context, stopwatch.Elapsed, value is null or DBNull ? 0 : 1, null), cancellationToken).ConfigureAwait(false);
            return value;
        }
        catch (Exception ex)
        {
            await ForgeEnterpriseRuntime.OnFailedAsync(command, new ForgeCommandExecutionResult(context, stopwatch.Elapsed, null, ex), cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private static DbCommand CreateCommand<T>(DbConnection connection, ForgeCompiledQueryPlan<T> plan, object? parameters, DbTransaction? transaction, int? timeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = plan.CommandType;
        if (transaction is not null) command.Transaction = transaction;
        if (timeoutSeconds.HasValue) command.CommandTimeout = timeoutSeconds.Value;
        ForgeCommandParameterLayout.Prepare(command, plan.ParameterNames);
        plan.Binder(command, parameters);
        NormalizeRawEnumPredicates<T>(command);
        NormalizeRawEnumStringParameters<T>(command);
        NormalizeRawEnumStringLiterals<T>(command);
        return command;
    }


    /// <summary>
    /// Handles raw SQL enum predicates without attributes and without knowing the physical column type.
    /// Example:
    ///     WHERE Status = @status
    /// becomes on SQL Server:
    ///     WHERE (TRY_CONVERT(bigint, Status) = @status OR CONVERT(nvarchar(128), Status) = @status__enum_text)
    /// This allows both INT enum storage and NVARCHAR enum storage (Paid) to work from the same raw query.
    /// </summary>
    private static void NormalizeRawEnumPredicates<T>(DbCommand command)
    {
        if (command.CommandType != CommandType.Text || string.IsNullOrWhiteSpace(command.CommandText) || command.Parameters.Count == 0)
            return;

        if (!IsSqlServer(command.Connection))
            return;

        var enumMap = ForgeRawEnumParameterMap<T>.Map;
        if (enumMap.Count == 0)
            return;

        for (var i = 0; i < command.Parameters.Count; i++)
        {
            if (command.Parameters[i] is not DbParameter parameter)
                continue;

            var logicalParameterName = parameter.ParameterName.TrimStart('@', ':');
            var enumType = ResolveEnumTypeForParameter(logicalParameterName, enumMap);
            if (enumType is null)
                continue;

            if (!TryGetEnumText(parameter.Value, enumType, out var enumText))
                continue;

            var textParameterName = ForgeParameterBinderCompiler.NormalizeParameterName(logicalParameterName + "__enum_text");
            if (!HasParameter(command, textParameterName))
            {
                var textParameter = command.CreateParameter();
                textParameter.ParameterName = textParameterName;
                textParameter.DbType = DbType.String;
                textParameter.Value = enumText;
                command.Parameters.Add(textParameter);
            }

            command.CommandText = RewriteEnumComparison(command.CommandText, parameter.ParameterName, textParameterName, enumMap);
        }
    }

    private static Type? ResolveEnumTypeForParameter(string parameterName, System.Collections.Generic.IReadOnlyDictionary<string, Type> enumMap)
    {
        if (enumMap.TryGetValue(parameterName, out var direct))
            return direct;

        // Common raw SQL style: new { status } for property/column Status.
        foreach (var item in enumMap)
        {
            if (string.Equals(item.Key, parameterName, StringComparison.OrdinalIgnoreCase))
                return item.Value;
        }

        return null;
    }

    private static bool TryGetEnumText(object? value, Type enumType, out string enumText)
    {
        enumText = string.Empty;
        if (value is null || value is DBNull)
            return false;

        if (value is string s)
        {
            if (!Enum.TryParse(enumType, s, ignoreCase: true, out var parsed) || parsed is null)
                return false;
            enumText = Enum.GetName(enumType, parsed) ?? s;
            return true;
        }

        if (value.GetType().IsEnum)
        {
            enumText = Enum.GetName(enumType, value) ?? value.ToString() ?? string.Empty;
            return enumText.Length > 0;
        }

        try
        {
            var numeric = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
            var enumValue = Enum.ToObject(enumType, numeric);
            enumText = Enum.GetName(enumType, enumValue) ?? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            return enumText.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string RewriteEnumComparison(
        string sql,
        string parameterName,
        string textParameterName,
        System.Collections.Generic.IReadOnlyDictionary<string, Type> enumMap)
    {
        var parameterToken = Regex.Escape(parameterName);
        const string identifier = @"(?:\[[^\]]+\]|\b[A-Za-z_][A-Za-z0-9_]*\b)";
        var columnPattern = $@"(?<column>{identifier}(?:\s*\.\s*{identifier})?)";

        return Regex.Replace(
            sql,
            $@"{columnPattern}\s*=\s*{parameterToken}(?![A-Za-z0-9_])",
            match =>
            {
                var column = match.Groups["column"].Value;
                var simpleColumnName = ExtractSimpleSqlIdentifier(column);
                if (!enumMap.ContainsKey(simpleColumnName))
                    return match.Value;

                return $"(TRY_CONVERT(bigint, {column}) = {parameterName} OR CONVERT(nvarchar(128), {column}) = {textParameterName})";
            },
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string ExtractSimpleSqlIdentifier(string sqlIdentifier)
    {
        var text = sqlIdentifier.Trim();
        var dot = text.LastIndexOf('.');
        if (dot >= 0)
            text = text[(dot + 1)..].Trim();

        if (text.Length >= 2 && text[0] == '[' && text[^1] == ']')
            text = text[1..^1];

        return text;
    }

    private static bool IsSqlServer(DbConnection? connection)
    {
        if (connection is null)
            return false;

        var name = connection.GetType().FullName ?? connection.GetType().Name;
        return name.Contains("SqlClient", StringComparison.OrdinalIgnoreCase)
            || name.Contains("SqlConnection", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasParameter(DbCommand command, string parameterName)
    {
        var normalized = parameterName.TrimStart('@', ':');
        for (var i = 0; i < command.Parameters.Count; i++)
        {
            if (command.Parameters[i] is not DbParameter p)
                continue;

            if (string.Equals(p.ParameterName.TrimStart('@', ':'), normalized, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void NormalizeRawEnumStringLiterals<T>(DbCommand command)
    {
        if (command.CommandType != CommandType.Text || string.IsNullOrWhiteSpace(command.CommandText) || !command.CommandText.Contains('\''))
            return;

        if (!IsSqlServer(command.Connection))
            return;

        var enumMap = ForgeRawEnumParameterMap<T>.Map;
        if (enumMap.Count == 0)
            return;

        const string identifier = @"(?:\[[^\]]+\]|\b[A-Za-z_][A-Za-z0-9_]*\b)";
        var columnPattern = $@"(?<column>{identifier}(?:\s*\.\s*{identifier})?)";

        command.CommandText = Regex.Replace(
            command.CommandText,
            $@"{columnPattern}\s*=\s*'(?<value>[^']+)'",
            match =>
            {
                var column = match.Groups["column"].Value;
                var simpleColumnName = ExtractSimpleSqlIdentifier(column);
                if (!enumMap.TryGetValue(simpleColumnName, out var enumType))
                    return match.Value;

                var text = match.Groups["value"].Value;
                if (!Enum.TryParse(enumType, text, ignoreCase: true, out var parsed) || parsed is null)
                    return match.Value;

                var numeric = Convert.ToInt64(parsed, System.Globalization.CultureInfo.InvariantCulture);
                var escaped = text.Replace("'", "''");
                return $"(TRY_CONVERT(bigint, {column}) = {numeric.ToString(System.Globalization.CultureInfo.InvariantCulture)} OR CONVERT(nvarchar(128), {column}) = N'{escaped}')";
            },
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static void NormalizeRawEnumStringParameters<T>(DbCommand command)
    {
        if (command.Parameters.Count == 0)
            return;

        var enumMap = ForgeRawEnumParameterMap<T>.Map;
        if (enumMap.Count == 0)
            return;

        for (var i = 0; i < command.Parameters.Count; i++)
        {
            if (command.Parameters[i] is not DbParameter parameter)
                continue;

            if (parameter.Value is not string text || string.IsNullOrWhiteSpace(text))
                continue;

            var name = parameter.ParameterName.TrimStart('@', ':');
            if (!enumMap.TryGetValue(name, out var enumType))
                continue;

            if (!Enum.TryParse(enumType, text, ignoreCase: true, out var parsed) || parsed is null)
                continue;

            var underlying = Enum.GetUnderlyingType(enumType);
            parameter.Value = Convert.ChangeType(parsed, underlying, System.Globalization.CultureInfo.InvariantCulture) ?? DBNull.Value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EstimateCapacity(string sql)
        => sql.Contains("TOP 1", StringComparison.OrdinalIgnoreCase) || sql.Contains("LIMIT 1", StringComparison.OrdinalIgnoreCase) ? 1 : 32;
}

internal static class ForgeRawEnumParameterMap<T>
{
    internal static readonly System.Collections.Generic.IReadOnlyDictionary<string, Type> Map = Build();

    private static System.Collections.Generic.IReadOnlyDictionary<string, Type> Build()
    {
        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (properties.Length == 0)
            return new System.Collections.Generic.Dictionary<string, Type>(0, StringComparer.OrdinalIgnoreCase);

        var result = new System.Collections.Generic.Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (!type.IsEnum)
                continue;

            result[property.Name] = type;

            // Also support [ForgeColumn("StatusId")] / [Column("StatusId")] style attributes without
            // referencing those attribute types directly from this hot-path helper.
            foreach (var attribute in property.GetCustomAttributes(inherit: true))
            {
                var attrType = attribute.GetType();
                var nameProperty = attrType.GetProperty("Name") ?? attrType.GetProperty("ColumnName");
                if (nameProperty?.GetValue(attribute) is string columnName && !string.IsNullOrWhiteSpace(columnName))
                    result[columnName] = type;
            }
        }

        return result.Count == 0
            ? new System.Collections.Generic.Dictionary<string, Type>(0, StringComparer.OrdinalIgnoreCase)
            : result;
    }
}
