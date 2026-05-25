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
