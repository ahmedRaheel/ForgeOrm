using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeVectorQuery<T>
{
    private readonly ForgeDb _db;
    internal ForgeVectorQuery(ForgeDb db) => _db = db;

    public async ValueTask<IReadOnlyList<ForgeVectorMatch<T>>> SearchAsync(float[] queryEmbedding, int topK = 10, VectorMetric metric = VectorMetric.Cosine, CancellationToken ct = default)
    {
        var rows = await _db.Set<T>().Take(topK).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select((x, i) => new ForgeVectorMatch<T>(x, 1d / (i + 1))).ToArray();
    }
}
