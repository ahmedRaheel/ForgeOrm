using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForgeORM.Telemetry;

public sealed record ForgeMonitoringSnapshot(int TotalQueries, int FailedQueries, double AverageMilliseconds, IReadOnlyList<ForgeQueryTelemetryEvent> SlowQueries);
