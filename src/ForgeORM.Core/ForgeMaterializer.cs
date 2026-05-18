using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>
/// High-performance row materializer with schema-aware plan caching.
/// The hot path avoids per-row property discovery, dictionaries, and PropertyInfo.SetValue.
/// </summary>
internal static class ForgeMaterializer
{
    private static readonly ConcurrentDictionary<string, MaterializerPlan> Plans = new(StringComparer.Ordinal);

    /// <summary>
    /// Maps the current data reader row to <typeparamref name="T"/>.
    /// </summary>
    public static T Map<T>(DbDataReader reader)
    {
        return (T)Map(typeof(T), reader)!;
    }

    /// <summary>
    /// Maps the current data reader row to the requested type.
    /// </summary>
    public static object? Map(Type type, DbDataReader reader)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (IsSimple(actualType))
        {
            var value = reader.IsDBNull(0) ? null : reader.GetValue(0);
            return ForgeValueConverter.FromDatabase(value, actualType);
        }

        var plan = GetPlan(actualType, reader);

        if (plan.Constructor is not null)
            return MapByConstructor(plan, reader);

        var instance = plan.CreateInstance();

        foreach (var property in plan.Properties)
        {
            var ordinal = property.Ordinal;

            if (ordinal < 0 || reader.IsDBNull(ordinal))
                continue;

            var dbValue = reader.GetValue(ordinal);
            var value = ForgeValueConverter.FromDatabase(dbValue, property.PropertyType);
            property.Setter(instance, value);
        }

        return instance;
    }

    private static object MapByConstructor(MaterializerPlan plan, DbDataReader reader)
    {
        var args = plan.ConstructorParameters;
        var values = new object?[args.Length];

        for (var i = 0; i < args.Length; i++)
        {
            var parameter = args[i];
            var ordinal = parameter.Ordinal;

            if (ordinal < 0 || reader.IsDBNull(ordinal))
            {
                values[i] = GetDefault(parameter.ParameterType);
                continue;
            }

            var dbValue = reader.GetValue(ordinal);
            values[i] = ForgeValueConverter.FromDatabase(dbValue, parameter.ParameterType);
        }

        return plan.Constructor!.Invoke(values);
    }

    private static MaterializerPlan GetPlan(Type type, DbDataReader reader)
    {
        var key = BuildPlanKey(type, reader);
        return Plans.GetOrAdd(key, _ => BuildPlan(type, reader));
    }

    private static string BuildPlanKey(Type type, DbDataReader reader)
    {
        var names = new string[reader.FieldCount + 1];
        names[0] = type.FullName ?? type.Name;

        for (var i = 0; i < reader.FieldCount; i++)
            names[i + 1] = reader.GetName(i);

        return string.Join("|", names);
    }

    private static MaterializerPlan BuildPlan(Type type, DbDataReader reader)
    {
        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);

        if (parameterlessCtor is not null)
        {
            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanWrite && IsScalarColumn(property.PropertyType))
                .Select(property => new PropertySetterPlan(
                    FindOrdinal(reader, property.Name),
                    property.PropertyType,
                    CompileSetter(property)))
                .Where(plan => plan.Ordinal >= 0)
                .ToArray();

            return new MaterializerPlan(
                type,
                static state => Activator.CreateInstance((Type)state!)!,
                type,
                null,
                Array.Empty<ConstructorParameterPlan>(),
                properties);
        }

        var ctor = type.GetConstructors()
            .OrderByDescending(x => x.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"No constructor found for {type.Name}");

        var constructorParameters = ctor.GetParameters()
            .Select(parameter => new ConstructorParameterPlan(
                FindOrdinal(reader, parameter.Name!),
                parameter.ParameterType))
            .ToArray();

        return new MaterializerPlan(
            type,
            static state => Activator.CreateInstance((Type)state!)!,
            type,
            ctor,
            constructorParameters,
            Array.Empty<PropertySetterPlan>());
    }

    private static Action<object, object?> CompileSetter(PropertyInfo property)
    {
        var target = Expression.Parameter(typeof(object), "target");
        var value = Expression.Parameter(typeof(object), "value");

        var convertedTarget = Expression.Convert(target, property.DeclaringType!);
        var convertedValue = Expression.Convert(value, property.PropertyType);
        var assign = Expression.Assign(Expression.Property(convertedTarget, property), convertedValue);

        return Expression.Lambda<Action<object, object?>>(assign, target, value).Compile();
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
            || type == typeof(TimeSpan);
    }

    private static bool IsScalarColumn(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsEnum
            || type.IsPrimitive
            || type == typeof(string)
            || type == typeof(Guid)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan);
    }

    private static object? GetDefault(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null)
            return null;

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private sealed record MaterializerPlan(
        Type Type,
        Func<object?, object> Factory,
        object FactoryState,
        ConstructorInfo? Constructor,
        ConstructorParameterPlan[] ConstructorParameters,
        PropertySetterPlan[] Properties)
    {
        public object CreateInstance() => Factory(FactoryState);
    }

    private sealed record PropertySetterPlan(
        int Ordinal,
        Type PropertyType,
        Action<object, object?> Setter);

    private sealed record ConstructorParameterPlan(
        int Ordinal,
        Type ParameterType);
}
