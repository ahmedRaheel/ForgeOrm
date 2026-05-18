using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

internal static class ForgeMaterializer
{
    private static readonly ConcurrentDictionary<Type, MaterializerTypePlan> TypePlanCache = new();
    private static readonly ConcurrentDictionary<string, MaterializerResultPlan> ResultPlanCache = new(StringComparer.Ordinal);

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
        var resultPlan = GetResultPlan(type, reader);

        for (var i = 0; i < resultPlan.Columns.Length; i++)
        {
            var column = resultPlan.Columns[i];
            if (column.Setter is null || reader.IsDBNull(column.Ordinal))
                continue;

            var dbValue = reader.GetValue(column.Ordinal);
            var value = ForgeValueConverter.FromDatabase(dbValue, column.Setter.PropertyType);
            column.Setter.Setter(instance, value);
        }

        return instance;
    }

    private static MaterializerResultPlan GetResultPlan(Type type, DbDataReader reader)
    {
        var key = BuildResultPlanKey(type, reader);
        return ResultPlanCache.GetOrAdd(key, _ => BuildResultPlan(type, reader));
    }

    private static string BuildResultPlanKey(Type type, DbDataReader reader)
    {
        var parts = new string[reader.FieldCount + 1];
        parts[0] = type.FullName ?? type.Name;
        for (var i = 0; i < reader.FieldCount; i++)
            parts[i + 1] = reader.GetName(i);
        return string.Join("|", parts);
    }

    private static MaterializerResultPlan BuildResultPlan(Type type, DbDataReader reader)
    {
        var typePlan = TypePlanCache.GetOrAdd(type, BuildTypePlan);
        var columns = new MaterializerColumn[reader.FieldCount];

        for (var i = 0; i < reader.FieldCount; i++)
        {
            var name = reader.GetName(i);
            typePlan.SettersByColumn.TryGetValue(name, out var setter);
            columns[i] = new MaterializerColumn(i, setter);
        }

        return new MaterializerResultPlan(columns);
    }

    private static MaterializerTypePlan BuildTypePlan(Type type)
    {
        var setters = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && IsScalar(p.PropertyType))
            .Select(p => new PropertySetter(
                p.GetCustomAttribute<ForgeORM.Abstractions.ForgeColumnAttribute>()?.Name ?? p.Name,
                p.PropertyType,
                BuildSetter(type, p)))
            .ToArray();

        return new MaterializerTypePlan(
            setters.ToDictionary(x => x.ColumnName, x => x, StringComparer.OrdinalIgnoreCase));
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
    private sealed record MaterializerTypePlan(IReadOnlyDictionary<string, PropertySetter> SettersByColumn);
    private sealed record MaterializerResultPlan(MaterializerColumn[] Columns);
    private sealed record MaterializerColumn(int Ordinal, PropertySetter? Setter);
}
