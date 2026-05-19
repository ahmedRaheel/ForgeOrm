using System.Collections.Concurrent;
using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Caches result-shape ordinals by entity + column names/types so generated and RuntimeEmit readers
/// bind by column name, not fragile SELECT ordinal order.
/// </summary>
public static class ForgeReaderShapeCache
{
    private static readonly ConcurrentDictionary<string, ForgeReaderShape> Shapes = new(StringComparer.Ordinal);

    public static ForgeReaderShape GetOrAdd(Type resultType, DbDataReader reader)
    {
        var key = CreateKey(resultType, reader);
        return Shapes.GetOrAdd(key, _ => Build(resultType, reader));
    }

    public static string CreateKey(Type resultType, DbDataReader reader)
    {
        var parts = new string[(reader.FieldCount * 2) + 1];
        parts[0] = resultType.FullName ?? resultType.Name;
        var index = 1;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            parts[index++] = reader.GetName(i);
            parts[index++] = reader.GetFieldType(i).FullName ?? reader.GetFieldType(i).Name;
        }
        return string.Join('|', parts);
    }

    private static ForgeReaderShape Build(Type resultType, DbDataReader reader)
    {
        var ordinals = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            ordinals[reader.GetName(i)] = i;
        return new ForgeReaderShape(resultType, ordinals);
    }
}

public sealed record ForgeReaderShape(Type ResultType, IReadOnlyDictionary<string, int> Ordinals)
{
    public bool TryGetOrdinal(string columnName, out int ordinal) => Ordinals.TryGetValue(columnName, out ordinal);
}
