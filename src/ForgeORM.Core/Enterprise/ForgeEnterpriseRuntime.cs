using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Process-wide enterprise runtime switches and low-overhead diagnostics for ForgeORM.
/// Everything is opt-in. When no interceptor/metrics feature is enabled, the pipeline only pays one branch check.
/// </summary>
public static class ForgeEnterpriseRuntime
{
    private static readonly object Gate = new();
    private static IForgeCommandInterceptor[] _interceptors = Array.Empty<IForgeCommandInterceptor>();
    private static readonly ConcurrentDictionary<string, ForgeQueryMetric> Metrics = new(StringComparer.Ordinal);

    /// <summary>Gets or sets whether normalized query metrics are recorded.</summary>
    public static bool MetricsEnabled { get; set; }

    /// <summary>Gets or sets the slow query threshold. Null disables slow-query interception metadata.</summary>
    public static TimeSpan? SlowQueryThreshold { get; set; } = TimeSpan.FromMilliseconds(250);

    /// <summary>True when at least one enterprise hook is enabled.</summary>
    public static bool IsEnabled => MetricsEnabled || _interceptors.Length != 0;

    /// <summary>Registers an enterprise command interceptor.</summary>
    public static void AddInterceptor(IForgeCommandInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        lock (Gate)
        {
            var next = new IForgeCommandInterceptor[_interceptors.Length + 1];
            Array.Copy(_interceptors, next, _interceptors.Length);
            next[^1] = interceptor;
            _interceptors = next;
        }
    }

    /// <summary>Clears all registered interceptors.</summary>
    public static void ClearInterceptors()
    {
        lock (Gate) _interceptors = Array.Empty<IForgeCommandInterceptor>();
    }

    /// <summary>Returns a snapshot of aggregated query metrics.</summary>
    public static IReadOnlyList<ForgeQueryMetric> GetMetricsSnapshot()
        => Metrics.Values.OrderByDescending(x => x.AverageElapsed).ToArray();

    /// <summary>Clears collected query metrics.</summary>
    public static void ClearMetrics() => Metrics.Clear();

    /// <summary>Creates a command context for the current command and plan.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ForgeCommandExecutionContext CreateContext(
        DbCommand command,
        ForgeCommandOperation operation,
        Type? resultType,
        Type? parameterType,
        string queryFingerprint)
    {
        var provider = command.Connection?.GetType().FullName ?? command.Connection?.GetType().Name ?? "unknown";
        return new ForgeCommandExecutionContext(
            provider,
            command.CommandText,
            command.CommandType,
            operation,
            resultType,
            parameterType,
            command.Parameters.Count,
            queryFingerprint);
    }

    /// <summary>Runs pre-execution interceptors when enterprise runtime is enabled.</summary>
    public static async ValueTask OnExecutingAsync(DbCommand command, ForgeCommandExecutionContext context, CancellationToken cancellationToken)
    {
        var interceptors = _interceptors;
        for (var i = 0; i < interceptors.Length; i++)
            await interceptors[i].CommandExecutingAsync(command, context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Records success and runs post-execution interceptors when enterprise runtime is enabled.</summary>
    public static async ValueTask OnExecutedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken)
    {
        if (MetricsEnabled) RecordMetric(result);
        var interceptors = _interceptors;
        for (var i = 0; i < interceptors.Length; i++)
            await interceptors[i].CommandExecutedAsync(command, result, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Records failure and runs failure interceptors when enterprise runtime is enabled.</summary>
    public static async ValueTask OnFailedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken)
    {
        if (MetricsEnabled) RecordMetric(result);
        var interceptors = _interceptors;
        for (var i = 0; i < interceptors.Length; i++)
            await interceptors[i].CommandFailedAsync(command, result, cancellationToken).ConfigureAwait(false);
    }

    private static void RecordMetric(ForgeCommandExecutionResult result)
    {
        var metric = Metrics.GetOrAdd(result.Context.QueryFingerprint, static (_, state) => new ForgeQueryMetric
        {
            QueryFingerprint = state.Context.QueryFingerprint,
            Provider = state.Context.Provider,
            Sql = state.Context.Sql
        }, result);

        metric.Record(result.Elapsed, result.Exception is not null);
    }
}
