using System.Collections.Concurrent;
using System.Diagnostics;

namespace ForgeORM.Core;

public sealed record ForgeQueryMetricV2(
    string SqlHash,
    string? QueryTag,
    TimeSpan Duration,
    int? Rows,
    bool Success,
    string? Error,
    DateTimeOffset TimestampUtc);

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
        //_metrics.Enqueue(new ForgeQueryMetric(hash, tag, stopwatch.Elapsed, rows, success, error?.GetType().Name + ": " + error?.Message, DateTimeOffset.UtcNow));
        while (_metrics.Count > MaxMetrics && _metrics.TryDequeue(out _)) { }
    }
}
