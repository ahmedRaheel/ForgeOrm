using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeSearchIndex
{
    private readonly ConcurrentDictionary<string, ForgeSearchDocument> _docs = new(StringComparer.OrdinalIgnoreCase);

    public void Upsert(ForgeSearchDocument document) => _docs[document.Id] = document;

    public IReadOnlyList<ForgeSearchDocument> Search(string text)
        => _docs.Values
            .Where(x => x.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToList();
}
