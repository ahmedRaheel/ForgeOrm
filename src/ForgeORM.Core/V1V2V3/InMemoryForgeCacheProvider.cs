using System.Collections.Concurrent;
using System.Text.Json;
using ForgeORM.Abstractions;
using AbstractionTenantContext = ForgeORM.Abstractions.ForgeTenantContext;
using AbstractionOutboxMessage = ForgeORM.Abstractions.ForgeOutboxMessage;

namespace ForgeORM.Core;

public sealed class InMemoryForgeCacheProvider : IForgeCacheProvider
{
    private sealed record CacheItem(object? Value, DateTimeOffset ExpiresAt);
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_cache.TryGetValue(key, out var item))
            return ValueTask.FromResult(default(T));

        if (item.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _cache.TryRemove(key, out _);
            return ValueTask.FromResult(default(T));
        }

        return ValueTask.FromResult((T?)item.Value);
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
    public ValueTask SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _cache[key] = new CacheItem(value, DateTimeOffset.UtcNow.Add(ttl));
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
        _cache.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }
}
