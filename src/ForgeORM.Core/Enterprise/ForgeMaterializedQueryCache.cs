using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Materialized query cache manager foundation.
/// </summary>
public sealed class ForgeMaterializedQueryCache
{
    private readonly ConcurrentDictionary<string, ForgeMaterializedQuery> _queries = new(StringComparer.OrdinalIgnoreCase);

    public ForgeMaterializedQuery Register(string name, string sql, TimeSpan refreshInterval)
    {
        var query = new ForgeMaterializedQuery(name, sql, DateTimeOffset.UtcNow, null, refreshInterval);
        _queries[name] = query;
        return query;
    }

    public IReadOnlyList<ForgeMaterializedQuery> All() => _queries.Values.ToList();

    public bool Invalidate(string name) => _queries.TryRemove(name, out _);
}
