namespace ForgeORM.Abstractions;

public interface IForgeCompiledQueryCache
/// <summary>
/// Defines the TryGet operation.
/// </summary>
/// <param name="key">The key value.</param>
/// <param name="sql">The sql value.</param>
/// <returns>The result of the TryGet operation.</returns>
{
    /// <summary>
    /// Defines the TryGet operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the TryGet operation.</returns>
    bool TryGet(ForgeCompiledQueryKey key, out string sql);
    /// <summary>
    /// Defines the Set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="sql">The sql value.</param>
    void Set(ForgeCompiledQueryKey key, string sql);
    /// <summary>
    /// Defines the Clear operation.
    /// </summary>
    void Clear();
}
