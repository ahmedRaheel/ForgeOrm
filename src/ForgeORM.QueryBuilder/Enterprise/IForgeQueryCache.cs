namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Defines enterprise query cache operations.
/// </summary>
public interface IForgeQueryCache
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync<T>(string key, T value, ForgeQueryCacheOptions options, CancellationToken cancellationToken = default);
    ValueTask RemoveRegionAsync(string region, CancellationToken cancellationToken = default);
}
