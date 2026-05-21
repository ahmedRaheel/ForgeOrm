using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
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

        await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        var rows = new List<T>(EstimateCapacity(sql));

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            rows.Add(materializer(reader));

        return rows;
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

        await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        var rows = new List<T>(EstimateCapacity(sql));

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            rows.Add(materializer(reader));

        return rows;
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

        await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            yield return materializer(reader);
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

        await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? materializer(reader) : default;
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

        await using var reader = await command.ExecuteReaderAsync(plan.Behavior, cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            return default;

        var materializer = ForgeCompiledReaderResolver.GetReader<T>(reader);
        var first = materializer(reader);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Sequence contains more than one element.");
        return first;
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

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
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

        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (value is null || value is DBNull) return default;

        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (target.IsEnum)
        {
            if (value is string text)
                return Enum.TryParse(target, text, true, out var parsed) ? (T)parsed! : default;
            return (T)Enum.ToObject(target, value);
        }

        return value is T typed
            ? typed
            : (T)Convert.ChangeType(value, target, System.Globalization.CultureInfo.InvariantCulture);
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

    private static DbCommand CreateCommand<T>(DbConnection connection, ForgeCompiledQueryPlan<T> plan, object? parameters, DbTransaction? transaction, int? timeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = plan.CommandType;
        if (transaction is not null) command.Transaction = transaction;
        if (timeoutSeconds.HasValue) command.CommandTimeout = timeoutSeconds.Value;
        ForgeCommandParameterLayout.Prepare(command, plan.ParameterNames);
        plan.Binder(command, parameters);
        return command;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EstimateCapacity(string sql)
        => sql.Contains("TOP 1", StringComparison.OrdinalIgnoreCase) || sql.Contains("LIMIT 1", StringComparison.OrdinalIgnoreCase) ? 1 : 32;
}
