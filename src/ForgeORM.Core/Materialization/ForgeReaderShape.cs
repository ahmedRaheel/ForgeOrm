using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

public sealed record ForgeReaderShape(Type ResultType, IReadOnlyDictionary<string, int> Ordinals)
{
    public bool TryGetOrdinal(string columnName, out int ordinal)
        => Ordinals.TryGetValue(columnName, out ordinal)
           || Ordinals.TryGetValue(ForgeColumnOrdinalShapeCache.Normalize(columnName), out ordinal);
}
