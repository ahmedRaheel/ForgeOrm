using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Shard router foundation.
/// </summary>
public sealed class ForgeShardRouter
{
    private readonly ConcurrentDictionary<string, ForgeShardDescriptor> _shards = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ForgeShardDescriptor shard)
        => _shards[shard.Name] = shard;

    public IReadOnlyList<ForgeShardDescriptor> All()
        => _shards.Values.OrderBy(x => x.Name).ToList();

    public ForgeShardDescriptor? Resolve(string name)
        => _shards.TryGetValue(name, out var shard) ? shard : null;
}
