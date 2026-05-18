using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

internal static class ForgeMaterializer
{
    private static readonly ConcurrentDictionary<Type, MaterializerPlan> Plans = new();

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

        var plan = Plans.GetOrAdd(actualType, BuildPlan);
        return plan.Map(reader);
    }

    private static MaterializerPlan BuildPlan(Type type)
    {
        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);

        if (parameterlessCtor is not null)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite && IsScalarColumnType(x.PropertyType))
                .Select(x => new PropertyMap(
                    x.Name,
                    x.PropertyType,
                    CreateSetter(type, x)))
                .ToArray();

            return new PropertyMaterializerPlan(type, properties);
        }

        var ctor = type.GetConstructors()
            .OrderByDescending(x => x.GetParameters().Length)
            .FirstOrDefault();

        if (ctor is null)
            throw new InvalidOperationException($"No constructor found for {type.Name}");

        return new ConstructorMaterializerPlan(ctor);
    }

    private static Action<object, object?> CreateSetter(Type declaringType, PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");

        var convertedInstance = Expression.Convert(instance, declaringType);
        var convertedValue = Expression.Convert(value, property.PropertyType);
        var assign = Expression.Assign(Expression.Property(convertedInstance, property), convertedValue);

        return Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
    }

    private sealed class PropertyMaterializerPlan : MaterializerPlan
    {
        private readonly Type _type;
        private readonly PropertyMap[] _properties;

        public PropertyMaterializerPlan(Type type, PropertyMap[] properties)
        {
            _type = type;
            _properties = properties;
        }

        public override object Map(DbDataReader reader)
        {
            var instance = Activator.CreateInstance(_type)!;

            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (reader.IsDBNull(i))
                    continue;

                var column = reader.GetName(i);

                for (var p = 0; p < _properties.Length; p++)
                {
                    var property = _properties[p];

                    if (!string.Equals(property.Name, column, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var dbValue = reader.GetValue(i);
                    var value = ForgeValueConverter.FromDatabase(dbValue, property.PropertyType);

                    if (value is not null)
                        property.Setter(instance, value);

                    break;
                }
            }

            return instance;
        }
    }

    private sealed class ConstructorMaterializerPlan : MaterializerPlan
    {
        private readonly ConstructorInfo _ctor;
        private readonly ParameterInfo[] _parameters;

        public ConstructorMaterializerPlan(ConstructorInfo ctor)
        {
            _ctor = ctor;
            _parameters = ctor.GetParameters();
        }

        public override object Map(DbDataReader reader)
        {
            var values = new object?[_parameters.Length];

            for (var i = 0; i < _parameters.Length; i++)
            {
                var parameter = _parameters[i];
                var ordinal = FindOrdinal(reader, parameter.Name!);

                if (ordinal < 0 || reader.IsDBNull(ordinal))
                {
                    values[i] = GetDefault(parameter.ParameterType);
                    continue;
                }

                var dbValue = reader.GetValue(ordinal);
                values[i] = ForgeValueConverter.FromDatabase(dbValue, parameter.ParameterType);
            }

            return _ctor.Invoke(values);
        }
    }

    private abstract class MaterializerPlan
    {
        public abstract object Map(DbDataReader reader);
    }

    private sealed record PropertyMap(
        string Name,
        Type PropertyType,
        Action<object, object?> Setter);

    private static int FindOrdinal(DbDataReader reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly)
            || type == typeof(TimeSpan)
            || type == typeof(byte[]);
    }

    private static bool IsScalarColumnType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return IsSimple(type);
    }

    private static object? GetDefault(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
            return null;

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
