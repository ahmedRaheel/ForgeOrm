using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeGraphPathQuery<TTo>
{
    private readonly ForgeGraphPath _path;
    internal ForgeGraphPathQuery(ForgeDb db, string from, object fromId, string? edge, string to, object toId)
    {
        _path = new ForgeGraphPath([new ForgeGraphPathNode(from, fromId), new ForgeGraphPathNode(to, toId)], [new ForgeGraphPathEdge(edge ?? "RELATED")]);
    }
    public ValueTask<IReadOnlyList<ForgeGraphPath>> ToListAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyList<ForgeGraphPath>>([_path]);
}
