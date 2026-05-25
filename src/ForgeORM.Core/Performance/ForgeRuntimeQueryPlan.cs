using System.Collections.Concurrent;
using System.Data;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Cached command shape. This avoids repeatedly classifying SQL text / command type / result shape in hot paths.
/// </summary>
public sealed record ForgeRuntimeQueryPlan(
    string CacheKey,
    string Sql,
    CommandType CommandType,
    Type ResultType,
    bool Buffered,
    DateTimeOffset CompiledAtUtc);
