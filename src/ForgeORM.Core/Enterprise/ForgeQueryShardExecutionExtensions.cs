using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public static class ForgeQueryShardExecutionExtensions
{
    public static ForgeShardQueryable<T> UseShard<T>(this ForgeShardQueryable<T> query, string shard)
    {
        query.Inner.ExecutionOptions.AddShard(shard);
        return query;
    }
}
