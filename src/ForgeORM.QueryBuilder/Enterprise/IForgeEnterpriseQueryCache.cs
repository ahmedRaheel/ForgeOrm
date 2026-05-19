namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Defines enterprise query cache operations.
/// </summary>
public interface IForgeEnterpriseQueryCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, ForgeQueryCacheOptions options, CancellationToken cancellationToken = default);
    Task RemoveRegionAsync(string region, CancellationToken cancellationToken = default);
}
