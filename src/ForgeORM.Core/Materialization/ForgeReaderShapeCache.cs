using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Caches result-shape ordinals by provider + entity + column names/types/nullability.
/// Uses the same non-cryptographic shape fingerprint as the materializer cache and avoids string.Join/array allocations.
/// </summary>
public static class ForgeReaderShapeCache
{
    private static readonly ConcurrentDictionary<ForgeColumnShapeKey, ForgeReaderShape> Shapes = new();

    public static ForgeReaderShape GetOrAdd(Type resultType, DbDataReader reader)
    {
        var shape = DbDataReaderShape.From(resultType, reader);
        var key = new ForgeColumnShapeKey(shape.ProviderName, resultType, ForgeFastHash.FingerprintReaderShape(shape));
        return Shapes.GetOrAdd(key, _ => Build(resultType, reader));
    }

    /// <summary>
    /// Compatibility key for existing MSIL caches. It is now a compact fingerprint, not a joined column string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static string CreateKey(Type resultType, DbDataReader reader)
    {
        var shape = DbDataReaderShape.From(resultType, reader);
        return ForgeFastHash.FingerprintReaderShape(shape);
    }

    private static ForgeReaderShape Build(Type resultType, DbDataReader reader)
    {
        var ordinals = new Dictionary<string, int>(reader.FieldCount * 2, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (string.IsNullOrWhiteSpace(name)) continue;
            ordinals.TryAdd(name, i);
            ordinals.TryAdd(ForgeColumnOrdinalShapeCache.Normalize(name), i);
        }
        return new ForgeReaderShape(resultType, ordinals);
    }
}
