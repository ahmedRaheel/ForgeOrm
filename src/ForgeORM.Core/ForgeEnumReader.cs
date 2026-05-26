using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Central enum materializer used by provider-specific and RuntimeEmit readers.
/// It is intentionally storage-agnostic: SQL INT, BIGINT, SMALLINT and NVARCHAR/VARCHAR enum names
/// all materialize into enum properties without requiring [ForgeEnumStorage].
/// </summary>
public static class ForgeEnumReader
{
    private static readonly ConcurrentDictionary<Type, Type> UnderlyingTypes = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TEnum ReadEnum<TEnum>(DbDataReader reader, int ordinal)
        where TEnum : struct, Enum
    {
        if (reader.IsDBNull(ordinal))
            return default;

        var value = reader.GetValue(ordinal);
        return ConvertEnumValue<TEnum>(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TEnum? ReadNullableEnum<TEnum>(DbDataReader reader, int ordinal)
        where TEnum : struct, Enum
    {
        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetValue(ordinal);
        return ConvertEnumValue<TEnum>(value);
    }

    /// <summary>
    /// Converts a provider value into an enum using Dapper-like tolerant behavior.
    /// Strings may be enum names or numeric text; numbers may be any provider numeric type.
    /// </summary>
    public static TEnum ConvertEnumValue<TEnum>(object? value)
        where TEnum : struct, Enum
    {
        if (value is null or DBNull)
            return default;

        if (value is TEnum typed)
            return typed;

        if (value is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return default;

            if (Enum.TryParse<TEnum>(text.Trim(), ignoreCase: true, out var parsed))
                return parsed;

            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericText))
                return (TEnum)Enum.ToObject(typeof(TEnum), numericText);

            throw new InvalidCastException($"Value '{text}' cannot be converted to enum {typeof(TEnum).FullName}.");
        }

        var enumType = typeof(TEnum);
        var underlying = UnderlyingTypes.GetOrAdd(enumType, static t => Enum.GetUnderlyingType(t));
        var numeric = Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
        return (TEnum)Enum.ToObject(enumType, numeric!);
    }
}
