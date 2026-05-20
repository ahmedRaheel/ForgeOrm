using System;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Central enum reader used by RuntimeEmit and source-generated readers.
/// Handles numeric enum storage and string enum storage without boxing in the common numeric path.
/// </summary>
public static class ForgeEnumReader
{
    public static TEnum ReadEnum<TEnum>(DbDataReader reader, int ordinal)
        where TEnum : struct, Enum
    {
        if (reader.IsDBNull(ordinal))
            return default;

        var value = reader.GetValue(ordinal);

        if (value is TEnum typed)
            return typed;

        if (value is string text)
            return Enum.Parse<TEnum>(text, ignoreCase: true);

        var underlying = Enum.GetUnderlyingType(typeof(TEnum));
        var converted = Convert.ChangeType(value, underlying);
        return (TEnum)Enum.ToObject(typeof(TEnum), converted!);
    }

    public static TEnum? ReadNullableEnum<TEnum>(DbDataReader reader, int ordinal)
        where TEnum : struct, Enum
    {
        if (reader.IsDBNull(ordinal))
            return null;

        return ReadEnum<TEnum>(reader, ordinal);
    }
}
