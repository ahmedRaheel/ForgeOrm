using System.Data;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Pre-builds ForgeORM runtime metadata, IL property accessors and SQL fragments for the supplied entity types.
    /// Call this at application startup to remove first-hit reflection from request paths.
    /// </summary>
    /// <param name="entityTypes">Entity types to compile into runtime plans.</param>
    public void PreWarm(params Type[] entityTypes) => ForgeRuntimeEntityMetadataCache.PreWarm(entityTypes);

    /// <summary>
    /// Streams rows using SequentialAccess and the cached MSIL materializer. This is the preferred API for very large result sets.
    /// </summary>
    public async IAsyncEnumerable<T> QueryStreamAsync<T>(
        string sql,
        object? parameters = null,
        int? timeoutSeconds = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await foreach (var row in ForgePerformancePipeline.StreamAsync<T>(connection, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            yield return row;
        }
    }

    /// <summary>
    /// Executes a SQL query through the centralized Forge performance pipeline.
    /// </summary>
    public async Task<IReadOnlyList<T>> QueryFastV1Async<T>(
        string sql,
        object? parameters = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ForgePerformancePipeline.QueryAsync<T>(connection, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a non-query command through the centralized Forge performance pipeline and cached MSIL parameter binder.
    /// </summary>
    public async Task<int> ExecuteFastAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ForgePerformancePipeline.ExecuteAsync(connection, sql, parameters, commandType: commandType, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Inserts an entity using the cached runtime entity plan and MSIL parameter binder.
    /// </summary>
    public Task<int> InsertCompiledAsync<T>(T entity, CancellationToken cancellationToken = default)
    {
        var plan = ForgeRuntimeEntityMetadataCache.For<T>();
        return ExecuteFastAsync(plan.InsertSql, entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates an entity using the cached runtime entity plan and MSIL parameter binder.
    /// </summary>
    public Task<int> UpdateCompiledAsync<T>(T entity, CancellationToken cancellationToken = default)
    {
        var plan = ForgeRuntimeEntityMetadataCache.For<T>();
        return ExecuteFastAsync(plan.UpdateSql, entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes an entity by its key using the cached runtime entity plan.
    /// </summary>
    public Task<int> DeleteCompiledAsync<T>(object id, CancellationToken cancellationToken = default)
    {
        var plan = ForgeRuntimeEntityMetadataCache.For<T>();
        if (plan.Key is null)
            throw new InvalidOperationException($"No key column found for {typeof(T).Name}.");

        return ExecuteFastAsync(plan.DeleteSql, new Dictionary<string, object?> { [plan.Key.PropertyName] = id }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Reads one entity by key using cached SQL, cached parameters and cached MSIL materialization.
    /// </summary>
    public async Task<T?> GetByIdCompiledAsync<T>(object id, CancellationToken cancellationToken = default)
    {
        var plan = ForgeRuntimeEntityMetadataCache.For<T>();
        if (plan.Key is null)
            throw new InvalidOperationException($"No key column found for {typeof(T).Name}.");

        var sql = $"SELECT {plan.SelectColumnsSql} FROM {plan.TableName} WHERE {plan.Key.ColumnName} = @{plan.Key.PropertyName}";
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await ForgePerformancePipeline.FirstOrDefaultAsync<T>(connection, sql, new Dictionary<string, object?> { [plan.Key.PropertyName] = id }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns internal cache counters so benchmark/sample APIs can prove the hot path is cached.
    /// </summary>
    public object GetPerformanceCacheStats()
        => new
        {
            queryPlans = ForgeRuntimeQueryPlanCache.Count,
            note = "Reader materializers, parameter binders, entity plans and query plans are ConcurrentDictionary-backed and reused after first hit."
        };
}
