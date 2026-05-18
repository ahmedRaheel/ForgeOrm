using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

internal static class ForgeMaterializer
{
    private static readonly ConcurrentDictionary<Type, PropertySetter[]> PropertySetterCache = new();

    public static T Map<T>(DbDataReader reader)
    {
        return (T)Map(typeof(T), reader)!;
    }

    public static object? Map(Type type, DbDataReader reader)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (IsSimple(actualType))
        {
            var value = reader.IsDBNull(0) ? null : reader.GetValue(0);
            return ForgeValueConverter.FromDatabase(value, actualType);
        }

        var parameterlessCtor = actualType.GetConstructor(Type.EmptyTypes);
        return parameterlessCtor is not null
            ? MapByProperties(actualType, reader)
            : MapByConstructor(actualType, reader);
    }

    private static object MapByProperties(Type type, DbDataReader reader)
    {
        var instance = Activator.CreateInstance(type)!;
        var setters = PropertySetterCache.GetOrAdd(type, BuildPropertySetters);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.IsDBNull(i))
                continue;

            var column = reader.GetName(i);
            PropertySetter? matched = null;

            for (var j = 0; j < setters.Length; j++)
            {
                if (string.Equals(setters[j].ColumnName, column, StringComparison.OrdinalIgnoreCase))
                {
                    matched = setters[j];
                    break;
                }
            }

            if (matched is null)
                continue;

            var dbValue = reader.GetValue(i);
            var value = ForgeValueConverter.FromDatabase(dbValue, matched.PropertyType);
            matched.Setter(instance, value);
        }

        return instance;
    }

    private static PropertySetter[] BuildPropertySetters(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && IsScalar(p.PropertyType))
            .Select(p => new PropertySetter(
                p.GetCustomAttribute<ForgeORM.Abstractions.ForgeColumnAttribute>()?.Name ?? p.Name,
                p.PropertyType,
                BuildSetter(type, p)))
            .ToArray();
    }

    private static Action<object, object?> BuildSetter(Type declaringType, PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var convertedInstance = Expression.Convert(instance, declaringType);
        var convertedValue = Expression.Convert(value, property.PropertyType);
        var assign = Expression.Assign(Expression.Property(convertedInstance, property), convertedValue);
        return Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
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
            values[i] = ForgeValueConverter.FromDatabase(dbValue, parameter.ParameterType);
        }

        return ctor.Invoke(values);
    }

    private static int FindOrdinal(DbDataReader reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    internal static bool IsScalar(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive
               || actual.IsEnum
               || actual == typeof(string)
               || actual == typeof(Guid)
               || actual == typeof(decimal)
               || actual == typeof(DateTime)
               || actual == typeof(DateTimeOffset)
               || actual == typeof(DateOnly)
               || actual == typeof(TimeOnly)
               || actual == typeof(TimeSpan)
               || actual == typeof(byte[]);
    }

    private static bool IsSimple(Type type) => IsScalar(type);

    private static object? GetDefault(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
            return null;

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private sealed record PropertySetter(string ColumnName, Type PropertyType, Action<object, object?> Setter);
}
