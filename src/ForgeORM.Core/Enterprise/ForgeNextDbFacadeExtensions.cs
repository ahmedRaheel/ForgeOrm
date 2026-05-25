using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public static class ForgeNextDbFacadeExtensions
{
    public static ForgeVectorQuery<T> Vector<T>(this ForgeDb db) => new(db);

    public static ForgeGraphTraversalBuilder Graph(this ForgeDb db) => new(db);

    public static ForgeWorkflowFacade<TWorkflow> Workflow<TWorkflow>(this ForgeDb db) => new(db);

    public static ForgeRulesFacade Rules(this ForgeDb db) => new(db);

    public static ForgeCubeBuilder<T> Cube<T>(this ForgeDb db) => new(db);

    public static ForgeShardQueryable<T> UseShard<T>(this IForgeQuery<T> query, string shard)
    {
        query.ExecutionOptions.AddShard(shard);
        return new ForgeShardQueryable<T>(query);
    }

    public static async ValueTask<int> ReadIntoAsync<T>(this IForgeQuery<T> query, T[] buffer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var rows = await query.Take(buffer.Length).ToListAsync(cancellationToken).ConfigureAwait(false);
        var count = Math.Min(rows.Count, buffer.Length);
        for (var i = 0; i < count; i++) buffer[i] = rows[i];
        return count;
    }

    public static async ValueTask<int> ReadIntoAsync<TSource, TDestination>(this IForgeQuery<TSource> query, TDestination[] buffer, CancellationToken cancellationToken = default)
        where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var rows = await query.Take(buffer.Length).ToListAsync(cancellationToken).ConfigureAwait(false);
        var count = Math.Min(rows.Count, buffer.Length);
        var mapper = ForgePooledBufferMapper<TSource, TDestination>.Map;
        for (var i = 0; i < count; i++) buffer[i] = mapper(rows[i]);
        return count;
    }
}
