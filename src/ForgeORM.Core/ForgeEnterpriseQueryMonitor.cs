using System.Collections.Concurrent;

namespace ForgeORM.Core;

/// <summary>
/// Lightweight in-memory query monitor for samples and local diagnostics.
/// </summary>
public static class ForgeEnterpriseQueryMonitor
{
    private static readonly ConcurrentQueue<ForgeEnterpriseQueryMetric> Metrics = new();

    private const int MaxMetrics = 1000;

    public static void Record(ForgeEnterpriseQueryMetric metric)
    {
        Metrics.Enqueue(metric);

        while (Metrics.Count > MaxMetrics && Metrics.TryDequeue(out _))
        {
        }
    }

    public static IReadOnlyList<ForgeEnterpriseQueryMetric> Snapshot()
    {
        return Metrics.ToArray();
    }

    public static void Clear()
    {
        while (Metrics.TryDequeue(out _))
        {
        }
    }
}
