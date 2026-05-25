using System.Reflection;

namespace ForgeORM.Core;

internal static class ForgeValueConverter
{
    /// <summary>
    /// Executes the ToDatabase operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="declaredType">The declaredType value.</param>
    /// <returns>The result of the ToDatabase operation.</returns>
    public static object? ToDatabase(object? value, Type? declaredType = null)
    {
        if (value is null)
            return DBNull.Value;

        var type = Nullable.GetUnderlyingType(declaredType ?? value.GetType())
                   ?? declaredType
                   ?? value.GetType();

        if (type.IsEnum)
        {
            var storage = GetEnumStorage(type);

            return storage == ForgeEnumStorage.Number
                ? Convert.ChangeType(value, Enum.GetUnderlyingType(type))
                : value.ToString();
        }

        if (type == typeof(DateTime))
        {
            var dateTime = (DateTime)value;
            return dateTime == default || dateTime < new DateTime(1753, 1, 1)
                ? DateTime.UtcNow
                : dateTime;
        }

        if (type == typeof(DateTimeOffset))
        {
            var dateTimeOffset = (DateTimeOffset)value;
            return dateTimeOffset == default
                ? DateTimeOffset.UtcNow
                : dateTimeOffset;
        }

        if (type == typeof(DateOnly))
            return ((DateOnly)value).ToDateTime(TimeOnly.MinValue);

        if (type == typeof(TimeOnly))
            return ((TimeOnly)value).ToTimeSpan();

        return value;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the T operation.</returns>
    public static T? FromDatabase<T>(object? value)
    {
        return (T?)FromDatabase(value, typeof(T));
    }
    /// <summary>
    /// Executes the FromDatabase operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="targetType">The targetType value.</param>
    /// <returns>The result of the FromDatabase operation.</returns>
    public static object? FromDatabase(object? value, Type targetType)
    {
        if (value is null || value == DBNull.Value)
            return GetDefault(targetType);

        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (actualType.IsInstanceOfType(value))
            return value;

        if (actualType.IsEnum)
        {
            return ConvertEnum(value, actualType, targetType);
        }

        if (actualType == typeof(string))
            return value.ToString();

        if (actualType == typeof(Guid))
        {
            if (value is Guid guid)
                return guid;

            return Guid.Parse(value.ToString()!);
        }

        if (actualType == typeof(DateTimeOffset))
        {
            if (value is DateTimeOffset dto)
                return dto;

            if (value is DateTime dt)
                return new DateTimeOffset(dt);

            return DateTimeOffset.Parse(value.ToString()!);
        }

        if (actualType == typeof(DateTime))
        {
            if (value is DateTime dt)
                return dt;

            if (value is DateTimeOffset dto)
                return dto.DateTime;

            return DateTime.Parse(value.ToString()!);
        }

        if (actualType == typeof(DateOnly))
        {
            if (value is DateOnly dateOnly)
                return dateOnly;

            if (value is DateTime dt)
                return DateOnly.FromDateTime(dt);

            return DateOnly.Parse(value.ToString()!);
        }

        if (actualType == typeof(TimeOnly))
        {
            if (value is TimeOnly timeOnly)
                return timeOnly;

            if (value is TimeSpan ts)
                return TimeOnly.FromTimeSpan(ts);

            if (value is DateTime dt)
                return TimeOnly.FromDateTime(dt);

            return TimeOnly.Parse(value.ToString()!);
        }

        if (actualType == typeof(TimeSpan))
        {
            if (value is TimeSpan ts)
                return ts;

            return TimeSpan.Parse(value.ToString()!);
        }

        if (actualType == typeof(byte[]))
        {
            if (value is byte[] bytes)
                return bytes;
        }

        if (value is IConvertible)
            return Convert.ChangeType(value, actualType);

        throw new InvalidCastException(
            $"ForgeORM cannot convert database value of type '{value.GetType().FullName}' to '{actualType.FullName}'.");
    }
   

    private static object? ConvertEnum(object value, Type enumType, Type targetType)
    {
        var nullable = Nullable.GetUnderlyingType(targetType) is not null;
        var text = value.ToString()?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return nullable ? null : ForgeRuntimeAccessorCache.DefaultValue(enumType);
        }

        if (Enum.TryParse(enumType, text, ignoreCase: true, out var parsedByName))
        {
            return parsedByName;
        }

        // Support numeric enum storage even if the value came from a string column.
        var enumUnderlying = Enum.GetUnderlyingType(enumType);

        try
        {
            if (IsNumericText(text))
            {
                var numeric = Convert.ChangeType(text, enumUnderlying);
                return Enum.ToObject(enumType, numeric!);
            }

            if (value is IConvertible && value is not string)
            {
                var numeric = Convert.ChangeType(value, enumUnderlying);
                return Enum.ToObject(enumType, numeric!);
            }
        }
        catch
        {
            // fall through to fallback below
        }

        // Enterprise-friendly fallback:
        // If DB contains a value not currently present in enum, avoid crashing materialization.
        // This keeps search/report endpoints alive when seed data or legacy DB values drift.
        var names = Enum.GetNames(enumType);

        if (names.Any(x => x.Equals("Unknown", StringComparison.OrdinalIgnoreCase)))
        {
            return Enum.Parse(enumType, "Unknown", ignoreCase: true);
        }

        if (names.Any(x => x.Equals("None", StringComparison.OrdinalIgnoreCase)))
        {
            return Enum.Parse(enumType, "None", ignoreCase: true);
        }

        if (names.Any(x => x.Equals("Default", StringComparison.OrdinalIgnoreCase)))
        {
            return Enum.Parse(enumType, "Default", ignoreCase: true);
        }

        return ForgeRuntimeAccessorCache.DefaultValue(enumType);
    }

    private static bool IsNumericText(string value)
    {
        return long.TryParse(
            value,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out _);
    }

    private static ForgeEnumStorage GetEnumStorage(Type enumType)
    {
        var attr = enumType.GetCustomAttribute<ForgeEnumStorageAttribute>();

        // ForgeORM follows .NET/EF convention for writes: numeric enum storage by default.
        // Reads are storage-agnostic and accept both numeric and string database values.
        return attr?.Storage ?? ForgeEnumStorage.Number;
    }

    private static object? GetDefault(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);

        if (nullable is not null)
            return null;

        return ForgeRuntimeAccessorCache.DefaultValue(type);
    }
}
