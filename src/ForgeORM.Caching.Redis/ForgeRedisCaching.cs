using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Caching.Redis;

public sealed record ForgeCacheOptions(string KeyPrefix = "forgeorm", TimeSpan DefaultTtl = default)
{
    public TimeSpan EffectiveDefaultTtl => DefaultTtl == default ? TimeSpan.FromMinutes(10) : DefaultTtl;
}

public interface IForgeQueryCache
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="key">The key value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value value.</param>
    /// <param name="ttl">The ttl value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    ValueTask SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the RemoveAsync operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RemoveAsync operation.</returns>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="ttl">The ttl value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
}

public sealed class ForgeDistributedQueryCache : IForgeQueryCache
{
    private readonly IDistributedCache _cache;
    private readonly ForgeCacheOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Executes the ForgeDistributedQueryCache operation.
    /// </summary>
    /// <param name="cache">The cache value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the ForgeDistributedQueryCache operation.</returns>
    public ForgeDistributedQueryCache(IDistributedCache cache, ForgeCacheOptions? options = null)
    {
        _cache = cache;
        _options = options ?? new ForgeCacheOptions();
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await _cache.GetStringAsync(BuildKey(key), cancellationToken);
        return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
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
    public async ValueTask SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await _cache.SetStringAsync(BuildKey(key), json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? _options.EffectiveDefaultTtl
        }, cancellationToken);
    }

    /// <summary>
    /// Executes the RemoveAsync operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RemoveAsync operation.</returns>
    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(BuildKey(key), cancellationToken);

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

    private string BuildKey(string key) => $"{_options.KeyPrefix}:{key}";
}

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

public static class ForgeCachingServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeRedisQueryCaching operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the AddForgeRedisQueryCaching operation.</returns>
    public static IServiceCollection AddForgeRedisQueryCaching(this IServiceCollection services, ForgeCacheOptions? options = null)
    {
        services.AddSingleton(options ?? new ForgeCacheOptions());
        services.AddSingleton<IForgeQueryCache, ForgeDistributedQueryCache>();
        return services;
    }

    /// <summary>
    /// Executes the AddForgeMemoryQueryCaching operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeMemoryQueryCaching operation.</returns>
    public static IServiceCollection AddForgeMemoryQueryCaching(this IServiceCollection services)
    {
        services.AddSingleton<IForgeQueryCache, ForgeMemoryQueryCache>();
        return services;
    }
}
