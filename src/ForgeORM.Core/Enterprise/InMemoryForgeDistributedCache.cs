using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// In-memory distributed-cache-compatible implementation for samples.
/// </summary>
public sealed class InMemoryForgeDistributedCache : IForgeDistributedCache
{
    private sealed record Entry(object? Value, DateTimeOffset ExpiresAtUtc);
    private readonly ConcurrentDictionary<string, Entry> _items = new(StringComparer.OrdinalIgnoreCase);

    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_items.TryGetValue(key, out var entry) || entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            _items.TryRemove(key, out _);
            return ValueTask.FromResult(default(T));
        }

        return ValueTask.FromResult((T?)entry.Value);
    }

    public ValueTask SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _items[key] = new Entry(value, DateTimeOffset.UtcNow.Add(ttl));
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _items.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }
}
