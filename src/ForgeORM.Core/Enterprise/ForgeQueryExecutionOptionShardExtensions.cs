using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal static class ForgeQueryExecutionOptionShardExtensions
{
    private static readonly ConditionalWeakTable<ForgeQueryExecutionOptions, ForgeShardList> Shards = new();

    public static void AddShard(this ForgeQueryExecutionOptions options, string shard)
    {
        if (string.IsNullOrWhiteSpace(shard)) return;
        var list = Shards.GetValue(options, _ => new ForgeShardList());
        lock (list.Values)
        {
            if (!list.Values.Contains(shard, StringComparer.OrdinalIgnoreCase))
                list.Values.Add(shard);
        }
    }

    public static IReadOnlyList<string> GetShards(this ForgeQueryExecutionOptions options)
    {
        return Shards.TryGetValue(options, out var list) ? list.Values.ToArray() : Array.Empty<string>();
    }

    private sealed class ForgeShardList
    {
        public List<string> Values { get; } = [];
    }
}
