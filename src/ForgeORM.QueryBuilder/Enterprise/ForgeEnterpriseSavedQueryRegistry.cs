using System.Collections.Concurrent;

namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Stores reusable named queries.
/// </summary>
public sealed class ForgeEnterpriseSavedQueryRegistry
{
    private readonly ConcurrentDictionary<string, ForgeEnterpriseSavedQuery> _queries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers or replaces a saved query.
    /// </summary>
    public void Register(string name, Func<IReadOnlyDictionary<string, object?>, ForgeSqlQuery> factory)
    {
        _queries[name] = new ForgeEnterpriseSavedQuery { Name = name, Factory = factory };
    }

    /// <summary>
    /// Builds a saved query with runtime parameters.
    /// </summary>
    public ForgeSqlQuery Build(string name, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        if (!_queries.TryGetValue(name, out var query))
        {
            throw new KeyNotFoundException($"Saved query '{name}' is not registered.");
        }

        return query.Factory(parameters ?? new Dictionary<string, object?>());
    }
}
