using ForgeORM.Abstractions;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Core;

internal readonly record struct ForgeMaterializerCacheKey(Type Type, ForgeOrmCompilationMode Mode, ulong ShapeHash)
{
    public static ForgeMaterializerCacheKey Create(Type type, DbDataReader reader, ForgeOrmCompilationMode mode)
    {
        unchecked
        {
            var hash = 1469598103934665603UL;
            for (var i = 0; i < reader.FieldCount; i++)
            {
                Add(ref hash, reader.GetName(i));
                var fieldType = reader.GetFieldType(i);
                hash ^= (ulong)fieldType.TypeHandle.GetHashCode();
                hash *= 1099511628211UL;
            }
            return new ForgeMaterializerCacheKey(type, mode, hash);
        }
    }

    private static void Add(ref ulong hash, string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '_' || c == '-' || c == ' ' || c == '[' || c == ']' || c == '"') continue;
            hash ^= char.ToUpperInvariant(c);
            hash *= 1099511628211UL;
        }
    }
}
