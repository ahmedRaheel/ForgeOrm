using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Caching.Redis;

public sealed class ForgeMemoryQueryCache : IForgeQueryCache
{
    private readonly Dictionary<string, (DateTimeOffset Expiry, object Value)> _items = new();
    private readonly object _gate = new();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_items.TryGetValue(key, out var item)) return ValueTask.FromResult<T?>(default);
            if (item.Expiry < DateTimeOffset.UtcNow)
            {
                _items.Remove(key);
                return ValueTask.FromResult<T?>(default);
            }
            return ValueTask.FromResult((T?)item.Value);
        }
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value value.</param>
    /// <param name="ttl">The ttl value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        lock (_gate) _items[key] = (DateTimeOffset.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(10)), value!);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Executes the RemoveAsync operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RemoveAsync operation.</returns>
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_gate) _items.Remove(key);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="ttl">The ttl value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public async ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null) return cached;
        var value = await factory(cancellationToken);
        await SetAsync(key, value, ttl, cancellationToken);
        return value;
    }
}
