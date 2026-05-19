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

public static class ForgeRuntimeQueryPlanCache
{
    private static readonly ConcurrentDictionary<string, ForgeRuntimeQueryPlan> Cache = new(StringComparer.Ordinal);

    public static ForgeRuntimeQueryPlan For<TResult>(string sql, CommandType commandType = CommandType.Text, bool buffered = true)
    {
        var key = $"{typeof(TResult).AssemblyQualifiedName}|{(int)commandType}|{buffered}|{sql}";
        return Cache.GetOrAdd(key, _ => new ForgeRuntimeQueryPlan(key, sql, commandType, typeof(TResult), buffered, DateTimeOffset.UtcNow));
    }

    public static int Count => Cache.Count;
}
