using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    private static readonly ConcurrentDictionary<Type, ForgeFindPlan> FindPlanCache = new();

    /// <summary>
    /// Fast primary-key lookup path. Bypasses expression translation, include handling, graph logic, and query-state rendering.
    /// This is the path to benchmark against Dapper QueryFirst/QueryFirstOrDefault by primary key.
    /// </summary>
    public T? Find<T>(object id, int? timeoutSeconds = null)
    {
        using var connection = CreateConnection();
        connection.Open();
        return ForgeFastExecutor.QueryFirstById<T>(connection, GetFindPlan(typeof(T)), id, timeoutSeconds);
    }

    /// <summary>
    /// Fast primary-key lookup path. Bypasses expression translation, include handling, graph logic, and query-state rendering.
    /// This is the path to benchmark against Dapper QueryFirst/QueryFirstOrDefault by primary key.
    /// </summary>
    public async Task<T?> FindAsync<T>(object id, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await ForgeFastExecutor.QueryFirstByIdAsync<T>(connection, GetFindPlan(typeof(T)), id, timeoutSeconds, cancellationToken);
    }

    /// <summary>
    /// Fast raw SQL list path. Skips expression translation and navigation processing.
    /// </summary>
    public IReadOnlyList<T> QueryFast<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var connection = CreateConnection();
        connection.Open();
        return ForgeAdo.Query<T>(connection, sql, parameters, timeoutSeconds: timeoutSeconds);
    }

    /// <summary>
    /// Fast raw SQL list path. Skips expression translation and navigation processing.
    /// </summary>
    public async Task<IReadOnlyList<T>> QueryFastAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await ForgePerformancePipeline.QueryAsync<T>(connection, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Fast raw SQL single-row path. Skips expression translation and navigation processing.
    /// </summary>
    public T? QueryFirstFast<T>(string sql, object? parameters = null, int? timeoutSeconds = null)
    {
        using var connection = CreateConnection();
        connection.Open();
        return ForgePerformancePipeline.FirstOrDefaultAsync<T>(connection, sql, parameters, timeoutSeconds: timeoutSeconds).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Fast raw SQL single-row path. Skips expression translation and navigation processing.
    /// </summary>
    public async Task<T?> QueryFirstFastAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await ForgePerformancePipeline.FirstOrDefaultAsync<T>(connection, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken);
    }

    private ForgeFindPlan GetFindPlan(Type entityType)
    {
        return FindPlanCache.GetOrAdd(entityType, BuildFindPlan);
    }

    private ForgeFindPlan BuildFindPlan(Type entityType)
    {
        var meta = _metadata.Resolve(entityType);
        var columns = meta.Properties
            .Where(x => !x.IsComputed)
            .Select(x => x.ColumnName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var columnList = columns.Length == 0 ? "*" : string.Join(", ", columns.Select(QuoteIdentifier));
        var table = QuoteIdentifier(meta.TableName);
        var key = QuoteIdentifier(meta.KeyColumn);

        var sql = $"SELECT TOP 1 {columnList} FROM {table} WHERE {key} = @Id";
        return new ForgeFindPlan(sql);
    }

    private static string QuoteIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("SQL identifier cannot be empty.", nameof(name));

        if (name.Contains('[') || name.Contains(']'))
            return name;

        if (name.Contains('.'))
            return string.Join('.', name.Split('.').Select(QuoteIdentifier));

        return "[" + name + "]";
    }
}

internal sealed record ForgeFindPlan(string Sql);

internal static class ForgeFastExecutor
{
    public static T? QueryFirstById<T>(DbConnection connection, ForgeFindPlan plan, object id, int? timeoutSeconds = null)
    {
        using var command = CreateFindCommand(connection, plan, id, timeoutSeconds);
        using var reader = command.ExecuteReader(CommandBehavior.SingleRow);
        return reader.Read() ? ForgeMaterializer.Map<T>(reader) : default;
    }

    public static async Task<T?> QueryFirstByIdAsync<T>(DbConnection connection, ForgeFindPlan plan, object id, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var command = CreateFindCommand(connection, plan, id, timeoutSeconds);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ForgeMaterializer.Map<T>(reader) : default;
    }

    private static DbCommand CreateFindCommand(DbConnection connection, ForgeFindPlan plan, object id, int? timeoutSeconds)
    {
        var command = connection.CreateCommand();
        command.CommandText = plan.Sql;
        command.CommandType = CommandType.Text;

        if (timeoutSeconds.HasValue)
            command.CommandTimeout = timeoutSeconds.Value;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@Id";
        parameter.Value = ForgeValueConverter.ToDatabase(id, id?.GetType()) ?? DBNull.Value;
        command.Parameters.Add(parameter);
        return command;
    }
}
