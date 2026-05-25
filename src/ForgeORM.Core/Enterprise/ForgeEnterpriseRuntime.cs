using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Describes the command operation currently being executed by ForgeORM.
/// </summary>
public enum ForgeCommandOperation
{
    Query = 0,
    FirstOrDefault = 1,
    SingleOrDefault = 2,
    Execute = 3,
    Scalar = 4,
    Stream = 5,
    Page = 6,
    Bulk = 7,
    Graph = 8
}

/// <summary>
/// Immutable command context passed to enterprise interceptors and diagnostics.
/// </summary>
public readonly record struct ForgeCommandExecutionContext(
    string Provider,
    string Sql,
    CommandType CommandType,
    ForgeCommandOperation Operation,
    Type? ResultType,
    Type? ParameterType,
    int ParameterCount,
    string QueryFingerprint);

/// <summary>
/// Immutable command result telemetry emitted after a command completes.
/// </summary>
public readonly record struct ForgeCommandExecutionResult(
    ForgeCommandExecutionContext Context,
    TimeSpan Elapsed,
    int? RowCount,
    Exception? Exception);

/// <summary>
/// Enterprise interception hook for auditing, tracing, multi-tenant enforcement,
/// slow query analysis, OpenTelemetry bridges and custom command policies.
/// Implementations should avoid heavy work in hot paths.
/// </summary>
public interface IForgeCommandInterceptor
{
    /// <summary>Runs before the command is executed.</summary>
    ValueTask CommandExecutingAsync(DbCommand command, ForgeCommandExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>Runs after the command completes successfully.</summary>
    ValueTask CommandExecutedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken = default);

    /// <summary>Runs when the command fails.</summary>
    ValueTask CommandFailedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated runtime metric for a normalized query fingerprint.
/// </summary>
public sealed class ForgeQueryMetric
{
    private long _count;
    private long _failed;
    private long _totalTicks;
    private long _maxTicks;
    private long _lastTicks;

    /// <summary>Normalized query fingerprint.</summary>
    public required string QueryFingerprint { get; init; }

    /// <summary>Provider name that produced the metric.</summary>
    public required string Provider { get; init; }

    /// <summary>Last observed SQL text for this fingerprint.</summary>
    public required string Sql { get; init; }

    /// <summary>Total successful or failed executions.</summary>
    public long Count => Interlocked.Read(ref _count);

    /// <summary>Total failed executions.</summary>
    public long Failed => Interlocked.Read(ref _failed);

    /// <summary>Average elapsed duration.</summary>
    public TimeSpan AverageElapsed => Count == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(Interlocked.Read(ref _totalTicks) / Count);

    /// <summary>Maximum elapsed duration.</summary>
    public TimeSpan MaxElapsed => TimeSpan.FromTicks(Interlocked.Read(ref _maxTicks));

    /// <summary>Last elapsed duration.</summary>
    public TimeSpan LastElapsed => TimeSpan.FromTicks(Interlocked.Read(ref _lastTicks));

    internal void Record(TimeSpan elapsed, bool failed)
    {
        Interlocked.Increment(ref _count);
        if (failed) Interlocked.Increment(ref _failed);
        Interlocked.Add(ref _totalTicks, elapsed.Ticks);
        Interlocked.Exchange(ref _lastTicks, elapsed.Ticks);

        var observed = elapsed.Ticks;
        while (true)
        {
            var current = Interlocked.Read(ref _maxTicks);
            if (observed <= current) return;
            if (Interlocked.CompareExchange(ref _maxTicks, observed, current) == current) return;
        }
    }
}

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

/// <summary>
/// Built-in interceptor that throws when a command crosses the configured slow query threshold.
/// Useful in CI/performance tests; avoid enabling it for normal production traffic unless intentional.
/// </summary>
public sealed class ForgeSlowQueryGuardInterceptor : IForgeCommandInterceptor
{
    private readonly TimeSpan _threshold;

    /// <summary>Creates a slow query guard interceptor.</summary>
    public ForgeSlowQueryGuardInterceptor(TimeSpan threshold) => _threshold = threshold;

    /// <inheritdoc />
    public ValueTask CommandExecutingAsync(DbCommand command, ForgeCommandExecutionContext context, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask CommandExecutedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken = default)
    {
        if (result.Elapsed > _threshold)
            throw new TimeoutException($"ForgeORM slow query guard failed. Elapsed={result.Elapsed.TotalMilliseconds:0.00}ms Threshold={_threshold.TotalMilliseconds:0.00}ms Fingerprint={result.Context.QueryFingerprint}");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask CommandFailedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
