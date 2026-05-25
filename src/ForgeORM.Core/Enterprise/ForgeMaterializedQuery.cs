using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Materialized query cache entry.
/// </summary>
public sealed record ForgeMaterializedQuery(
    string Name,
    string Sql,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastRefreshUtc,
    TimeSpan RefreshInterval);
