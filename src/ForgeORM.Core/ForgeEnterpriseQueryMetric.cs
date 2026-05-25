using System.Collections.Concurrent;

namespace ForgeORM.Core;

/// <summary>
/// Runtime metrics for high-volume query execution.
/// </summary>
public sealed record ForgeEnterpriseQueryMetric(
    string Name,
    string Sql,
    DateTimeOffset StartedAtUtc,
    TimeSpan Duration,
    int Rows,
    bool Throttled);
