using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Data.SqlClient;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Core.Performance;

internal readonly record struct SqlServerMaterializerKey(Type Type, ForgeOrmCompilationMode Mode, ulong ShapeHash)
{
    public static SqlServerMaterializerKey Create(Type type, SqlDataReader reader, ForgeOrmCompilationMode mode)
    {
        unchecked
        {
            var hash = 1469598103934665603UL;
            for (var i = 0; i < reader.FieldCount; i++)
            {
                Add(ref hash, reader.GetName(i));
                hash ^= (ulong)reader.GetFieldType(i).TypeHandle.GetHashCode();
                hash *= 1099511628211UL;
            }
            return new SqlServerMaterializerKey(type, mode, hash);
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
