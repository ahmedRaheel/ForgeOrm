using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Caching.Redis;

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
