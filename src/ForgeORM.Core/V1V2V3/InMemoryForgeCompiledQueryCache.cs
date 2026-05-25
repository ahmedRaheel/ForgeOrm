using System.Collections.Concurrent;
using System.Text.Json;
using ForgeORM.Abstractions;
using AbstractionTenantContext = ForgeORM.Abstractions.ForgeTenantContext;
using AbstractionOutboxMessage = ForgeORM.Abstractions.ForgeOutboxMessage;

namespace ForgeORM.Core;

public sealed class InMemoryForgeCompiledQueryCache : IForgeCompiledQueryCache
{
    private readonly ConcurrentDictionary<ForgeORM.Abstractions.ForgeCompiledQueryKey, string> _cache = new();

    /// <summary>
    /// Executes the TryGet operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the TryGet operation.</returns>
    public bool TryGet(ForgeORM.Abstractions.ForgeCompiledQueryKey key, out string sql)
        => _cache.TryGetValue(key, out sql!);

    /// <summary>
    /// Executes the Set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="sql">The sql value.</param>
    public void Set(ForgeORM.Abstractions.ForgeCompiledQueryKey key, string sql)
        => _cache[key] = sql;

    /// <summary>
    /// Executes the Clear operation.
    /// </summary>
    public void Clear() => _cache.Clear();

}
