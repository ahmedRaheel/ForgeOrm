using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal static class ForgeObjectMapper
{
    /// <summary>
    /// Executes the TTarget operation.
    /// </summary>
    /// <typeparam name="TTarget">The type used by the operation.</typeparam>
    /// <param name="new">The new value.</param>
    /// <returns>The result of the TTarget operation.</returns>
    public static TTarget Map<TTarget>(object source) where TTarget : new()
    {
        if (source is TTarget typed) return typed;
        var target = new TTarget();
        Copy(source, target);
        return target;
    }

    /// <summary>
    /// Executes the Copy operation.
    /// </summary>
    /// <param name="source">The source value.</param>
    /// <param name="target">The target value.</param>
    public static void Copy(object source, object target)
    {
        var sourceProps = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var targetProp in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            if (!sourceProps.TryGetValue(targetProp.Name, out var sourceProp)) continue;
            if (IsEnumerableButNotString(targetProp.PropertyType)) continue;
            var value = ForgeRuntimeAccessorCache.Get(sourceProp, source);
            ForgeRuntimeAccessorCache.Set(targetProp, target!, ConvertTo(value, targetProp.PropertyType));
        }
    }

    /// <summary>
    /// Executes the ConvertTo operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="targetType">The targetType value.</param>
    /// <returns>The result of the ConvertTo operation.</returns>
    public static object? ConvertTo(object? value, Type targetType)
    {
        if (value is null || value is DBNull)
        {
            var nullable = Nullable.GetUnderlyingType(targetType);
            if (nullable is not null || !targetType.IsValueType) return null;
            return ForgeRuntimeAccessorCache.DefaultValue(targetType);
        }

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type.IsEnum)
        {
            if (value is string text)
                return Enum.Parse(type, text, ignoreCase: true);
            return Enum.ToObject(type, value);
        }
        if (type == typeof(Guid)) return value is Guid g ? g : Guid.Parse(value.ToString()!);
        if (type == typeof(DateTimeOffset)) return value is DateTimeOffset dto ? dto : new DateTimeOffset(Convert.ToDateTime(value));
        if (type.IsAssignableFrom(value.GetType())) return value;
        return Convert.ChangeType(value, type);
    }

    private static bool IsScalarColumnType(Type type)
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

    private static bool IsEnumerableButNotString(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}
