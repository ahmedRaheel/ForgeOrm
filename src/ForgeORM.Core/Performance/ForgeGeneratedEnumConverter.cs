using System.Buffers;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

internal static class ForgeGeneratedEnumConverter<TEnum> where TEnum : struct, Enum
{
    private static readonly ConcurrentDictionary<string, TEnum> Names = new(StringComparer.OrdinalIgnoreCase);

    public static TEnum FromDatabase(object value)
    {
        if (value is TEnum typed) return typed;
        if (value is string text) return Names.GetOrAdd(text, static x => Enum.Parse<TEnum>(x, ignoreCase: true));
        return (TEnum)Enum.ToObject(typeof(TEnum), value);
    }

    public static object ToDatabase(TEnum value, bool storeAsNumber)
        => storeAsNumber ? Convert.ToInt64(value) : value.ToString();
}
