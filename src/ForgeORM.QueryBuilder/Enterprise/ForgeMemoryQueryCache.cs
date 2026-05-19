using System.Collections.Concurrent;

namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Lightweight in-memory query cache for dashboards and repeated lookups.
/// </summary>
public sealed class ForgeMemoryQueryCache : IForgeQueryCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _items = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_items.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return Task.FromResult((T?)entry.Value);
        }

        _items.TryRemove(key, out _);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, ForgeQueryCacheOptions options, CancellationToken cancellationToken = default)
    {
        _items[key] = new CacheEntry(value, DateTimeOffset.UtcNow.Add(options.Duration), options.Region);
        return Task.CompletedTask;
    }

    public Task RemoveRegionAsync(string region, CancellationToken cancellationToken = default)
    {
        foreach (var item in _items.Where(x => string.Equals(x.Value.Region, region, StringComparison.OrdinalIgnoreCase)).ToArray())
        {
            _items.TryRemove(item.Key, out _);
        }

        return Task.CompletedTask;
    }

    private sealed record CacheEntry(object? Value, DateTimeOffset ExpiresAt, string? Region);
}
