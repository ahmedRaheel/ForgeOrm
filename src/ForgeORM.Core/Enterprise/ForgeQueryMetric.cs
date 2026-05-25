using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

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
