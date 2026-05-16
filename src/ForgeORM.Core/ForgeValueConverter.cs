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
            if (value is string enumText)
                return Enum.Parse(actualType, enumText, ignoreCase: true);

            var enumUnderlying = Enum.GetUnderlyingType(actualType);
            var enumValue = Convert.ChangeType(value, enumUnderlying);
            return Enum.ToObject(actualType, enumValue!);
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
   
    private static ForgeEnumStorage GetEnumStorage(Type enumType)
    {
        var attr = enumType.GetCustomAttribute<ForgeEnumStorageAttribute>();

        return attr?.Storage ?? ForgeEnumStorage.String;
    }

    private static object? GetDefault(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);

        if (nullable is not null)
            return null;

        return type.IsValueType
            ? Activator.CreateInstance(type)
            : null;
    }
}

public enum ForgeEnumStorage
{
    String = 0,
    Number = 1
}

[AttributeUsage(AttributeTargets.Enum)]
public sealed class ForgeEnumStorageAttribute : Attribute
{
    /// <summary>
    /// Executes the ForgeEnumStorageAttribute operation.
    /// </summary>
    /// <param name="storage">The storage value.</param>
    /// <returns>The result of the ForgeEnumStorageAttribute operation.</returns>
    public ForgeEnumStorage Storage { get; }

    /// <summary>
    /// Executes the ForgeEnumStorageAttribute operation.
    /// </summary>
    /// <param name="storage">The storage value.</param>
    /// <returns>The result of the ForgeEnumStorageAttribute operation.</returns>
    public ForgeEnumStorageAttribute(ForgeEnumStorage storage)
    {
        Storage = storage;
    }
}