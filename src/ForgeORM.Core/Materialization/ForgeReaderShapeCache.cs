using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Caches result-shape ordinals by provider + entity + column names/types.
/// The materializer key intentionally avoids GetSchemaTable because that allocates heavily on SqlDataReader hot paths.
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
    /// Fast compatibility key for MSIL materializer caches. No GetSchemaTable, no array, no string.Join.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static string CreateKey(Type resultType, DbDataReader reader)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;

        Add(ref hash, reader.GetType().FullName ?? reader.GetType().Name, prime);
        Add(ref hash, resultType.FullName ?? resultType.Name, prime);

        var count = reader.FieldCount;
        hash ^= (ulong)count;
        hash *= prime;

        for (var i = 0; i < count; i++)
        {
            Add(ref hash, reader.GetName(i), prime);
            Type fieldType;
            try { fieldType = reader.GetFieldType(i); }
            catch { fieldType = typeof(object); }
            Add(ref hash, fieldType.FullName ?? fieldType.Name, prime);
        }

        return hash.ToString("X16", CultureInfo.InvariantCulture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, string? value, ulong prime)
    {
        if (string.IsNullOrEmpty(value))
        {
            hash *= prime;
            return;
        }

        for (var i = 0; i < value.Length; i++)
        {
            hash ^= char.ToUpperInvariant(value[i]);
            hash *= prime;
        }
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

public sealed record ForgeReaderShape(Type ResultType, IReadOnlyDictionary<string, int> Ordinals)
{
    public bool TryGetOrdinal(string columnName, out int ordinal)
        => Ordinals.TryGetValue(columnName, out ordinal)
           || Ordinals.TryGetValue(ForgeColumnOrdinalShapeCache.Normalize(columnName), out ordinal);
}
