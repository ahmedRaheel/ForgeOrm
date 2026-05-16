using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForgeORM.Telemetry;

public sealed record ForgeQueryTelemetryEvent(string Operation, string Sql, long ElapsedMilliseconds, bool Success, string? Error, DateTimeOffset TimestampUtc);
public sealed record ForgeMonitoringSnapshot(int TotalQueries, int FailedQueries, double AverageMilliseconds, IReadOnlyList<ForgeQueryTelemetryEvent> SlowQueries);

public interface IForgeTelemetry
{
    Activity? StartQueryActivity(string operation, string sql);
    void RecordQuery(string operation, string sql, TimeSpan elapsed, bool success, Exception? exception = null);
    ForgeMonitoringSnapshot Snapshot(int slowQueryLimit = 20);
}

public sealed class ForgeTelemetry : IForgeTelemetry
{
    public const string ActivitySourceName = "ForgeORM";
    public const string MeterName = "ForgeORM.Metrics";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> QueryCounter = Meter.CreateCounter<long>("forgeorm.query.count");
    private static readonly Histogram<double> QueryDuration = Meter.CreateHistogram<double>("forgeorm.query.duration.ms");
    private readonly ConcurrentQueue<ForgeQueryTelemetryEvent> _events = new();
    private readonly ILogger<ForgeTelemetry>? _logger;

    /// <summary>
    /// Initializes or executes the ForgeTelemetry operation.
    /// </summary>
    /// <param name="logger">The logger value.</param>
    public ForgeTelemetry(ILogger<ForgeTelemetry>? logger = null) => _logger = logger;

    /// <summary>
    /// Initializes or executes the StartQueryActivity operation.
    /// </summary>
    /// <param name="operation">The operation value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The operation result.</returns>
    public Activity? StartQueryActivity(string operation, string sql)
    {
        var activity = ActivitySource.StartActivity($"ForgeORM {operation}");
        activity?.SetTag("db.system", "sql");
        activity?.SetTag("db.statement", sql);
        activity?.SetTag("forgeorm.operation", operation);
        return activity;
    }

    /// <summary>
    /// Initializes or executes the RecordQuery operation.
    /// </summary>
    /// <param name="operation">The operation value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="elapsed">The elapsed value.</param>
    /// <param name="success">The success value.</param>
    /// <param name="exception">The exception value.</param>
    public void RecordQuery(string operation, string sql, TimeSpan elapsed, bool success, Exception? exception = null)
    {
        QueryCounter.Add(1, new KeyValuePair<string, object?>("operation", operation), new KeyValuePair<string, object?>("success", success));
        QueryDuration.Record(elapsed.TotalMilliseconds);
        var evt = new ForgeQueryTelemetryEvent(operation, sql, (long)elapsed.TotalMilliseconds, success, exception?.Message, DateTimeOffset.UtcNow);
        _events.Enqueue(evt);
        while (_events.Count > 1000 && _events.TryDequeue(out _)) { }
        if (!success) _logger?.LogError(exception, "ForgeORM query failed: {Operation}", operation);
        else if (elapsed.TotalMilliseconds > 500) _logger?.LogWarning("ForgeORM slow query {Elapsed}ms: {Sql}", elapsed.TotalMilliseconds, sql);
    }

    /// <summary>
    /// Initializes or executes the Snapshot operation.
    /// </summary>
    /// <param name="slowQueryLimit">The slowQueryLimit value.</param>
    /// <returns>The operation result.</returns>
    public ForgeMonitoringSnapshot Snapshot(int slowQueryLimit = 20)
    {
        var rows = _events.ToArray();
        return new ForgeMonitoringSnapshot(
            rows.Length,
            rows.Count(x => !x.Success),
            rows.Length == 0 ? 0 : rows.Average(x => x.ElapsedMilliseconds),
            rows.OrderByDescending(x => x.ElapsedMilliseconds).Take(slowQueryLimit).ToList());
    }
}

public static class ForgeTelemetryServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeTelemetry operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddForgeTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<IForgeTelemetry, ForgeTelemetry>();
        return services;
    }
}
