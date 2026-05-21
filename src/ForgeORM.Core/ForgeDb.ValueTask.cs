using System.Data.Common;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core;

public partial class ForgeDb
{
    /// <summary>
    /// Executes a SQL query using the ValueTask high-performance pipeline.
    /// Prefer this overload in hot paths to reduce Task allocation when the operation completes synchronously.
    /// </summary>
    public async ValueTask<IReadOnlyList<T>> QueryValueAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        return await ForgePerformancePipeline.QueryAsync<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a typed-parameter SQL query using the ValueTask high-performance pipeline.
    /// This avoids boxing the parameter container before binder resolution.
    /// </summary>
    public async ValueTask<IReadOnlyList<T>> QueryValueAsync<T, TParameters>(string sql, TParameters parameters, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        return await ForgePerformancePipeline.QueryAsync<T, TParameters>(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a scalar SQL command using the ValueTask high-performance pipeline.
    /// </summary>
    public async ValueTask<T?> ExecuteScalarValueAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        return await ForgePerformancePipeline.ExecuteScalarAsync<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Streams rows directly from the data reader without materializing a list.
    /// This is the lowest-allocation read API for large result sets.
    /// </summary>
    public async IAsyncEnumerable<T> StreamValueAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var c = CreateConnection();
        await foreach (var item in ForgePerformancePipeline.StreamAsync<T>(c, sql, parameters, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Warms a query plan and binder without executing SQL. Call from application startup for critical queries.
    /// </summary>
    public void WarmupQuery<T>(string sql, object? parameters = null)
    {
        using var c = CreateConnection();
        ForgeQueryPlanWarmup.WarmupQuery<T>(c, sql, parameters);
    }
}
