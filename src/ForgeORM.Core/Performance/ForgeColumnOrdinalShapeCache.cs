using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace ForgeORM.Core;

/// <summary>
/// Caches result-shape column ordinal maps by entity type and reader schema. This prevents repeated column-name scans
/// and makes reader materialization stable even when SQL column order changes.
/// </summary>
internal static class ForgeColumnOrdinalShapeCache
{
    private static readonly ConcurrentDictionary<ForgeColumnShapeKey, IReadOnlyDictionary<string, int>> Cache = new();

    public static IReadOnlyDictionary<string, int> GetOrAdd(Type entityType, DbDataReader reader)
    {
        var key = new ForgeColumnShapeKey(entityType, BuildShapeFingerprint(reader));
        return Cache.GetOrAdd(key, _ => BuildOrdinalMap(reader));
    }

    public static bool TryGetOrdinal(Type entityType, DbDataReader reader, string columnName, out int ordinal)
    {
        var map = GetOrAdd(entityType, reader);
        return map.TryGetValue(columnName, out ordinal);
    }

    private static IReadOnlyDictionary<string, int> BuildOrdinalMap(DbDataReader reader)
    {
        var map = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (!string.IsNullOrWhiteSpace(name))
                map.TryAdd(name, i);
        }
        return map;
    }

    private static string BuildShapeFingerprint(DbDataReader reader)
    {
        var builder = new StringBuilder(reader.FieldCount * 32);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            builder.Append(reader.GetName(i)).Append(':').Append(reader.GetFieldType(i).FullName).Append('|');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}

internal readonly record struct ForgeColumnShapeKey(Type EntityType, string ShapeFingerprint);
