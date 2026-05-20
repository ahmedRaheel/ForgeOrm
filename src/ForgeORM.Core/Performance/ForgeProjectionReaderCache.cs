using System.Collections.Concurrent;
using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed record ForgeProjectionPlan(Type SourceType, Type ProjectionType, string Fingerprint);

internal static class ForgeProjectionReaderCache
{
    private static readonly ConcurrentDictionary<string, ForgeProjectionPlan> Plans = new(StringComparer.Ordinal);

    public static ForgeProjectionPlan GetOrCreate(Type sourceType, Type projectionType, LambdaExpression? projection)
    {
        var fingerprint = string.Join('|', sourceType.FullName, projectionType.FullName, projection?.ToString() ?? "identity");
        return Plans.GetOrAdd(fingerprint, _ => new ForgeProjectionPlan(sourceType, projectionType, fingerprint));
    }
}

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

public interface IForgeProjectedQuery<TSource, TProjection>
{
    Task<IReadOnlyList<TProjection>> ToListAsync(CancellationToken cancellationToken = default);
}

internal sealed class ForgeProjectedQuery<TSource, TProjection> : IForgeProjectedQuery<TSource, TProjection>
{
    private readonly IForgeQuery<TSource> _source;
    private readonly Expression<Func<TSource, TProjection>> _projection;

    public ForgeProjectedQuery(IForgeQuery<TSource> source, Expression<Func<TSource, TProjection>> projection)
    {
        _source = source;
        _projection = projection;
    }

    public async Task<IReadOnlyList<TProjection>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _source.ToListAsync(cancellationToken).ConfigureAwait(false);
        var map = ForgeExpressionDelegateCache.Get(_projection);
        var result = new List<TProjection>(rows.Count);
        foreach (var row in rows)
            result.Add(map(row));
        return result;
    }
}
