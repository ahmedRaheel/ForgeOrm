using System.Collections.Concurrent;

namespace ForgeORM.Core.SavedQueries;

/// <summary>
/// In-memory registry for reusable query definitions.
/// </summary>
public sealed class ForgeSavedQueryRegistry
{
    private readonly ConcurrentDictionary<string, ForgeSavedQuery> _queries =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers or replaces a saved query.
    /// </summary>
    public ForgeSavedQueryRegistry Register(
        string name,
        string sql,
        object? parameters = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Saved query name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("Saved query SQL is required.", nameof(sql));
        }

        _queries[name] = new ForgeSavedQuery
        {
            Name = name,
            Sql = sql,
            Parameters = parameters,
            Description = description
        };

        return this;
    }

    /// <summary>
    /// Registers or replaces a saved query.
    /// </summary>
    public ForgeSavedQueryRegistry Register(
        ForgeSavedQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Register(query.Name, query.Sql, query.Parameters, query.Description);
    }

    /// <summary>
    /// Returns true when a saved query exists.
    /// </summary>
    public bool Contains(string name) => _queries.ContainsKey(name);

    /// <summary>
    /// Gets a saved query or throws when it is missing.
    /// </summary>
    public ForgeSavedQuery Get(string name)
    {
        if (_queries.TryGetValue(name, out var query))
        {
            return query;
        }

        throw new KeyNotFoundException($"Saved query '{name}' was not registered.");
    }

    /// <summary>
    /// Attempts to get a saved query.
    /// </summary>
    public bool TryGet(string name, out ForgeSavedQuery? query)
        => _queries.TryGetValue(name, out query);

    /// <summary>
    /// Removes a saved query.
    /// </summary>
    public bool Remove(string name) => _queries.TryRemove(name, out _);

    /// <summary>
    /// Lists saved query definitions.
    /// </summary>
    public IReadOnlyList<ForgeSavedQuery> List()
        => _queries.Values.OrderBy(x => x.Name).ToArray();
}
