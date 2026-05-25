namespace ForgeORM.Abstractions;

public interface IForgeCacheProvider
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
    ValueTask SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the RemoveAsync operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RemoveAsync operation.</returns>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}
