using System.Collections.Concurrent;
using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public interface IForgeProjectedQuery<TSource, TProjection>
{
    ValueTask<IReadOnlyList<TProjection>> ToListAsync(CancellationToken cancellationToken = default);
}
