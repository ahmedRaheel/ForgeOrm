using System.Collections.Concurrent;
using System.Diagnostics;

namespace ForgeORM.Core;

public sealed class ForgeQueryMetricV2 
{
    public string SqlHash { get; set; }
    public string? QueryTag { get; set; }
    public TimeSpan Duration { get; set; }
    public int? Rows { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }

    
}
   

/// <summary>
/// Lightweight in-process query monitor for slow query dashboards, heatmaps, and diagnostics.
/// </summary>
public static class ForgeQueryMonitor
{
    private static readonly ConcurrentQueue<ForgeQueryMetric> _metrics = new();
    private const int MaxMetrics = 2048;

    public static IReadOnlyList<ForgeQueryMetric> Snapshot() => _metrics.ToArray();

    public static void Record(string sql, string? tag, Stopwatch stopwatch, int? rows, bool success, Exception? error = null)
    {
        var hash = ForgeFastHash.FingerprintSql(sql);
        //_metrics.Enqueue(new () { SqlHash = hash, QueryTag = tag, Duration = stopwatch.Elapsed, Rows = rows, Success = success, Error = error?.GetType().Name + ": " + error?.Message, TimestampUtc = DateTimeOffset.UtcNow });
        while (_metrics.Count > MaxMetrics && _metrics.TryDequeue(out _)) { }
    }
}
