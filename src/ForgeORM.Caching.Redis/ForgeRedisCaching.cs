using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Caching.Redis;

public sealed record ForgeCacheOptions(string KeyPrefix = "forgeorm", TimeSpan DefaultTtl = default)
{
    public TimeSpan EffectiveDefaultTtl => DefaultTtl == default ? TimeSpan.FromMinutes(10) : DefaultTtl;
}

public interface IForgeQueryCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
}

public sealed class ForgeDistributedQueryCache : IForgeQueryCache
{
    private readonly IDistributedCache _cache;
    private readonly ForgeCacheOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ForgeDistributedQueryCache(IDistributedCache cache, ForgeCacheOptions? options = null)
    {
        _cache = cache;
        _options = options ?? new ForgeCacheOptions();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await _cache.GetStringAsync(BuildKey(key), cancellationToken);
        return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await _cache.SetStringAsync(BuildKey(key), json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? _options.EffectiveDefaultTtl
        }, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => _cache.RemoveAsync(BuildKey(key), cancellationToken);

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
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

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (!_items.TryGetValue(key, out var item)) return Task.FromResult<T?>(default);
            if (item.Expiry < DateTimeOffset.UtcNow)
            {
                _items.Remove(key);
                return Task.FromResult<T?>(default);
            }
            return Task.FromResult((T?)item.Value);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        lock (_gate) _items[key] = (DateTimeOffset.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(10)), value!);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        lock (_gate) _items.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
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
    public static IServiceCollection AddForgeRedisQueryCaching(this IServiceCollection services, ForgeCacheOptions? options = null)
    {
        services.AddSingleton(options ?? new ForgeCacheOptions());
        services.AddSingleton<IForgeQueryCache, ForgeDistributedQueryCache>();
        return services;
    }

    public static IServiceCollection AddForgeMemoryQueryCaching(this IServiceCollection services)
    {
        services.AddSingleton<IForgeQueryCache, ForgeMemoryQueryCache>();
        return services;
    }
}
