using System.Collections.Concurrent;
using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal sealed class ForgeProjectedQuery<TSource, TProjection> : IForgeProjectedQuery<TSource, TProjection>
{
    private readonly IForgeQuery<TSource> _source;
    private readonly Expression<Func<TSource, TProjection>> _projection;

    public ForgeProjectedQuery(IForgeQuery<TSource> source, Expression<Func<TSource, TProjection>> projection)
    {
        _source = source;
        _projection = projection;
    }

    public async ValueTask<IReadOnlyList<TProjection>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _source.ToListAsync(cancellationToken).ConfigureAwait(false);
        var map = ForgeExpressionDelegateCache.Get(_projection);
        var result = new List<TProjection>(rows.Count);
        foreach (var row in rows)
            result.Add(map(row));
        return result;
    }
}
