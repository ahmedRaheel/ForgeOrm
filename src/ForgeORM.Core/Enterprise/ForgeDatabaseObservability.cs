using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Enterprise observability monitor foundation.
/// </summary>
public static class ForgeDatabaseObservability
{
    private static readonly ConcurrentQueue<ForgeDatabaseMetric> Metrics = new();

    public static void Record(string name, double value, string unit)
        => Metrics.Enqueue(new ForgeDatabaseMetric(name, value, unit, DateTimeOffset.UtcNow));

    public static IReadOnlyList<ForgeDatabaseMetric> Snapshot()
        => Metrics.ToArray();
}
