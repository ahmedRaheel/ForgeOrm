using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

internal static class ForgeMaterializer
{
    private static readonly ConcurrentDictionary<Type, MaterializerTypePlan> TypePlans = new();

    /// <summary>
    /// Maps the current data-reader row to the requested type.
    /// The mapper caches entity metadata and compiled property setters so repeated reads avoid reflection scans.
    /// </summary>
    public static T Map<T>(DbDataReader reader)
    {
        return (T)Map(typeof(T), reader)!;
    }

    /// <summary>
    /// Maps the current data-reader row to the requested runtime type.
    /// </summary>
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

        var plan = TypePlans.GetOrAdd(actualType, BuildTypePlan);

        if (plan.HasParameterlessConstructor)
            return MapByProperties(plan, reader);

        return MapByConstructor(plan, reader);
    }

    private static object MapByProperties(MaterializerTypePlan plan, DbDataReader reader)
    {
        var instance = plan.CreateInstance();

        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.IsDBNull(i))
                continue;

            var column = reader.GetName(i);

            if (!plan.PropertiesByColumn.TryGetValue(column, out var property))
                continue;

            var dbValue = reader.GetValue(i);
            var value = ForgeValueConverter.FromDatabase(dbValue, property.PropertyType);
            property.Set(instance, value);
        }

        return instance;
    }

    private static object MapByConstructor(MaterializerTypePlan plan, DbDataReader reader)
    {
        if (plan.Constructor is null)
            throw new InvalidOperationException($"No constructor found for {plan.Type.Name}");

        var parameters = plan.ConstructorParameters;
        var values = new object?[parameters.Length];
        var ordinals = BuildOrdinalLookup(reader);

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (!ordinals.TryGetValue(parameter.Name!, out var ordinal) || reader.IsDBNull(ordinal))
            {
                values[i] = GetDefault(parameter.ParameterType);
                continue;
            }

            var dbValue = reader.GetValue(ordinal);
            values[i] = ForgeValueConverter.FromDatabase(dbValue, parameter.ParameterType);
        }

        return plan.Constructor.Invoke(values);
    }

    private static MaterializerTypePlan BuildTypePlan(Type type)
    {
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanWrite)
            .Select(MaterializerPropertyPlan.Create)
            .ToArray();

        var propertiesByColumn = new Dictionary<string, MaterializerPropertyPlan>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in properties)
            propertiesByColumn[property.PropertyName] = property;

        var ctor = type.GetConstructors()
            .OrderByDescending(x => x.GetParameters().Length)
            .FirstOrDefault();

        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);

        return new MaterializerTypePlan(
            type,
            parameterlessCtor,
            ctor,
            ctor?.GetParameters() ?? Array.Empty<ParameterInfo>(),
            propertiesByColumn);
    }

    private static Dictionary<string, int> BuildOrdinalLookup(DbDataReader reader)
    {
        var ordinals = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < reader.FieldCount; i++)
            ordinals[reader.GetName(i)] = i;

        return ordinals;
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

    private sealed class MaterializerTypePlan
    {
        private readonly Func<object>? _factory;

        public MaterializerTypePlan(
            Type type,
            ConstructorInfo? parameterlessConstructor,
            ConstructorInfo? constructor,
            ParameterInfo[] constructorParameters,
            IReadOnlyDictionary<string, MaterializerPropertyPlan> propertiesByColumn)
        {
            Type = type;
            ParameterlessConstructor = parameterlessConstructor;
            Constructor = constructor;
            ConstructorParameters = constructorParameters;
            PropertiesByColumn = propertiesByColumn;
            _factory = parameterlessConstructor is null ? null : CompileFactory(type);
        }

        public Type Type { get; }
        public ConstructorInfo? ParameterlessConstructor { get; }
        public ConstructorInfo? Constructor { get; }
        public ParameterInfo[] ConstructorParameters { get; }
        public IReadOnlyDictionary<string, MaterializerPropertyPlan> PropertiesByColumn { get; }
        public bool HasParameterlessConstructor => ParameterlessConstructor is not null;

        public object CreateInstance()
        {
            if (_factory is not null)
                return _factory();

            return Activator.CreateInstance(Type)!;
        }

        private static Func<object> CompileFactory(Type type)
        {
            var body = Expression.Convert(Expression.New(type), typeof(object));
            return Expression.Lambda<Func<object>>(body).Compile();
        }
    }

    private sealed class MaterializerPropertyPlan
    {
        private MaterializerPropertyPlan(
            string propertyName,
            Type propertyType,
            Action<object, object?> setter)
        {
            PropertyName = propertyName;
            PropertyType = propertyType;
            Set = setter;
        }

        public string PropertyName { get; }
        public Type PropertyType { get; }
        public Action<object, object?> Set { get; }

        public static MaterializerPropertyPlan Create(PropertyInfo property)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");

            var typedInstance = Expression.Convert(instance, property.DeclaringType!);
            var typedValue = Expression.Convert(value, property.PropertyType);
            var assign = Expression.Assign(Expression.Property(typedInstance, property), typedValue);

            var setter = Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();

            return new MaterializerPropertyPlan(
                property.Name,
                property.PropertyType,
                setter);
        }
    }
}
