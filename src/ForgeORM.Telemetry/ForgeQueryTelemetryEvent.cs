using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForgeORM.Telemetry;

public sealed record ForgeQueryTelemetryEvent(string Operation, string Sql, long ElapsedMilliseconds, bool Success, string? Error, DateTimeOffset TimestampUtc);
