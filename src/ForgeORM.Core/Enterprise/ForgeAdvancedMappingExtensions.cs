using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace ForgeORM.Core;

/// <summary>
/// Public advanced mapping helpers for DTO, nested object, enum, JSON, and dictionary based projections.
/// These helpers are intentionally dependency-free so they can be used by samples and applications.
/// </summary>
public static class ForgeAdvancedMappingExtensions
{
    public static TTarget MapTo<TTarget>(this object source)
        where TTarget : new()
    {
        if (source is TTarget typed)
        {
            return typed;
        }

        var target = new TTarget();
        CopyTo(source, target);
        return target;
    }

    public static void CopyTo(this object source, object target)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (target is null) throw new ArgumentNullException(nameof(target));

        var sourceProperties = source.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead)
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var targetProperty in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite))
        {
            if (!sourceProperties.TryGetValue(targetProperty.Name, out var sourceProperty))
            {
                continue;
            }

            if (IsEnumerableButNotString(targetProperty.PropertyType))
            {
                continue;
            }

            var value = ForgeRuntimeAccessorCache.Get(sourceProperty, source);
            ForgeRuntimeAccessorCache.Set(targetProperty, target, ConvertValue(value, targetProperty.PropertyType));
        }
    }

    public static Dictionary<string, object?> ToDictionaryProjection(this object source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        return source.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead && !IsEnumerableButNotString(x.PropertyType))
            .ToDictionary(x => x.Name, x => ForgeRuntimeAccessorCache.Get(x, source), StringComparer.OrdinalIgnoreCase);
    }

    public static TTarget FromDictionaryProjection<TTarget>(this IReadOnlyDictionary<string, object?> values)
        where TTarget : new()
    {
        var target = new TTarget();
        foreach (var property in typeof(TTarget).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite))
        {
            if (values.TryGetValue(property.Name, out var value))
            {
                ForgeRuntimeAccessorCache.Set(property, target, ConvertValue(value, property.PropertyType));
            }
        }

        return target;
    }

    public static TTarget FromJsonProjection<TTarget>(this string json)
        where TTarget : new()
    {
        return JsonSerializer.Deserialize<TTarget>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new TTarget();
    }

    public static string ToJsonProjection(this object source)
    {
        return JsonSerializer.Serialize(source);
    }

    public static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null || value is DBNull)
        {
            var nullable = Nullable.GetUnderlyingType(targetType);
            return nullable is not null || !targetType.IsValueType ? null : ForgeRuntimeAccessorCache.DefaultValue(targetType);
        }

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type.IsEnum)
        {
            return value is string text ? Enum.Parse(type, text, ignoreCase: true) : Enum.ToObject(type, value);
        }

        if (type == typeof(Guid)) return value is Guid g ? g : Guid.Parse(value.ToString()!);
        if (type == typeof(DateTimeOffset)) return value is DateTimeOffset dto ? dto : new DateTimeOffset(Convert.ToDateTime(value));
        if (type.IsAssignableFrom(value.GetType())) return value;
        return Convert.ChangeType(value, type);
    }

    private static bool IsEnumerableButNotString(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}
