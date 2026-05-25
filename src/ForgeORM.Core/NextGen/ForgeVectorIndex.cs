using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeVectorIndex
{
    private readonly ConcurrentDictionary<string, ForgeVectorDocument> _docs = new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(ForgeVectorDocument document) => _docs[document.Id] = document;

    public IReadOnlyList<(ForgeVectorDocument Document, float Score)> Search(float[] embedding, int topK = 5)
        => _docs.Values
            .Select(d => (d, ForgeVectorizedMath.CosineSimilarity(d.Embedding, embedding)))
            .OrderByDescending(x => x.Item2)
            .Take(topK)
            .Select(x => (x.d, x.Item2))
            .ToList();
}
