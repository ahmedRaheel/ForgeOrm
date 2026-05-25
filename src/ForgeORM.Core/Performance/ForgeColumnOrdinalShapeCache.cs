using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Caches result-shape column ordinal maps by provider, target type, column names, CLR types, provider DB types and nullability.
/// This prevents accidental materializer reuse across incompatible reader shapes and removes repeated name normalization scans.
/// </summary>
public static class ForgeColumnOrdinalShapeCache
{
    private static readonly ConcurrentDictionary<ForgeColumnShapeKey, IReadOnlyDictionary<string, int>> Cache = new();

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static IReadOnlyDictionary<string, int> GetOrAdd(Type targetType, DbDataReader reader)
    {
        var shape = DbDataReaderShape.From(targetType, reader);
        var key = new ForgeColumnShapeKey(shape.ProviderName, targetType, ForgeFastHash.FingerprintReaderShape(shape));
        return Cache.GetOrAdd(key, _ => BuildOrdinalMap(reader));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetOrdinal(Type targetType, DbDataReader reader, string columnName, out int ordinal)
    {
        var map = GetOrAdd(targetType, reader);
        return map.TryGetValue(Normalize(columnName), out ordinal) || map.TryGetValue(columnName, out ordinal);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOrdinalOrMinusOne(Type targetType, DbDataReader reader, string columnName)
        => TryGetOrdinal(targetType, reader, columnName, out var ordinal) ? ordinal : -1;

    private static IReadOnlyDictionary<string, int> BuildOrdinalMap(DbDataReader reader)
    {
        var map = new Dictionary<string, int>(reader.FieldCount * 2, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            map.TryAdd(name, i);
            map.TryAdd(Normalize(name), i);
        }
        return map;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        Span<char> buffer = value.Length <= 256 ? stackalloc char[value.Length] : new char[value.Length];
        var index = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '_' || c == '-' || c == ' ' || c == '[' || c == ']' || c == '"')
                continue;
            buffer[index++] = char.ToUpperInvariant(c);
        }
        return new string(buffer[..index]);
    }
}

public readonly record struct ForgeColumnShapeKey(string ProviderName, Type TargetType, string ShapeFingerprint);

public readonly record struct DbDataReaderColumnShape(string Name, Type ClrType, string DbTypeName, bool AllowDBNull);

public sealed record DbDataReaderShape(string ProviderName, Type TargetType, DbDataReaderColumnShape[] Columns)
{
    public static DbDataReaderShape From(Type targetType, DbDataReader reader)
    {
        var columns = new DbDataReaderColumnShape[reader.FieldCount];
        System.Data.DataTable? schema = null;
        try { schema = reader.GetSchemaTable(); } catch { }

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var dbTypeName = string.Empty;
            var allowNull = true;
            try { dbTypeName = reader.GetDataTypeName(i) ?? string.Empty; } catch { }
            try { allowNull = schema is not null && i < schema.Rows.Count && schema.Rows[i]["AllowDBNull"] is bool b ? b : true; } catch { }
            columns[i] = new DbDataReaderColumnShape(reader.GetName(i), reader.GetFieldType(i), dbTypeName, allowNull);
        }
        return new DbDataReaderShape(reader.GetType().FullName ?? reader.GetType().Name, targetType, columns);
    }
}
