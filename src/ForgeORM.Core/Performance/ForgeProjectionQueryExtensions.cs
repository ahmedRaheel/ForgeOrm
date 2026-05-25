using System.Collections.Concurrent;
using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public static class ForgeProjectionQueryExtensions
{
    /// <summary>
    /// Projection-only query hook. This caches the projection shape now and keeps the public surface ready for generated DTO readers.
    /// </summary>
    public static IForgeProjectedQuery<TSource, TProjection> Select<TSource, TProjection>(
        this IForgeQuery<TSource> query,
        Expression<Func<TSource, TProjection>> projection)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(projection);
        _ = ForgeProjectionReaderCache.GetOrCreate(typeof(TSource), typeof(TProjection), projection);
        return new ForgeProjectedQuery<TSource, TProjection>(query, projection);
    }
}
