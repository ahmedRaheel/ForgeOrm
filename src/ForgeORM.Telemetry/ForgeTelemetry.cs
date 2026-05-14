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

    public ForgeTelemetry(ILogger<ForgeTelemetry>? logger = null) => _logger = logger;

    public Activity? StartQueryActivity(string operation, string sql)
    {
        var activity = ActivitySource.StartActivity($"ForgeORM {operation}");
        activity?.SetTag("db.system", "sql");
        activity?.SetTag("db.statement", sql);
        activity?.SetTag("forgeorm.operation", operation);
        return activity;
    }

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
    public static IServiceCollection AddForgeTelemetry(this IServiceCollection services)
    {
        services.AddSingleton<IForgeTelemetry, ForgeTelemetry>();
        return services;
    }
}
