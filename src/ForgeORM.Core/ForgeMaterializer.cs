using System.Data.Common;
using System.Reflection;

namespace ForgeORM.Core;

internal static class ForgeMaterializer
{
    /// <summary>
    /// Initializes or executes the Map operation.
    /// </summary>
    /// <param name="reader">The reader value.</param>
    /// <returns>The operation result.</returns>
    public static T Map<T>(DbDataReader reader)
    {
        return (T)Map(typeof(T), reader)!;
    }

    /// <summary>
    /// Initializes or executes the Map operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <param name="reader">The reader value.</param>
    /// <returns>The operation result.</returns>
    public static object? Map(Type type, DbDataReader reader)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (IsSimple(actualType))
        {
            var value = reader.IsDBNull(0)
                ? null
                : reader.GetValue(0);

            return ForgeValueConverter.FromDatabase(value, actualType);
        }

        var parameterlessCtor = actualType.GetConstructor(Type.EmptyTypes);

        if (parameterlessCtor is not null)
            return MapByProperties(actualType, reader);

        return MapByConstructor(actualType, reader);
    }

    private static object MapByProperties(Type type, DbDataReader reader)
    {
        var instance = Activator.CreateInstance(type)!;

        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanWrite)
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var column = reader.GetName(i);

            if (!props.TryGetValue(column, out var property))
                continue;

            if (reader.IsDBNull(i))
                continue;

            var dbValue = reader.GetValue(i);

            var value = ForgeValueConverter.FromDatabase(
                dbValue,
                property.PropertyType);

            property.SetValue(instance, value);
        }

        return instance;
    }

    private static object MapByConstructor(Type type, DbDataReader reader)
    {
        var ctor = type.GetConstructors()
            .OrderByDescending(x => x.GetParameters().Length)
            .FirstOrDefault();

        if (ctor is null)
            throw new InvalidOperationException($"No constructor found for {type.Name}");

        var parameters = ctor.GetParameters();

        var values = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            var ordinal = FindOrdinal(reader, parameter.Name!);

            if (ordinal < 0 || reader.IsDBNull(ordinal))
            {
                values[i] = GetDefault(parameter.ParameterType);
                continue;
            }

            var dbValue = reader.GetValue(ordinal);

            values[i] = ForgeValueConverter.FromDatabase(
                dbValue,
                parameter.ParameterType);
        }

        return ctor.Invoke(values);
    }

    private static int FindOrdinal(DbDataReader reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(
                    reader.GetName(i),
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsSimple(Type type)
    {
        return
            type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(Guid) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan);
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