using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>
/// Compiled property getter cache used by provider bulk and graph paths.
/// It replaces PropertyInfo.GetValue in row loops and avoids reflection invocation per row.
/// </summary>
internal static class ForgeProviderAccessors
{
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> GetterCache = new();

    public static object? Get(PropertyInfo property, object target)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(target);
        return GetterCache.GetOrAdd(property, CreateGetter)(target);
    }

    public static Func<object, object?> CreateGetter(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (property.GetMethod is null)
            throw new InvalidOperationException($"Property '{property.Name}' does not have a getter.");

        var instance = Expression.Parameter(typeof(object), "instance");
        var typed = Expression.Convert(instance, property.DeclaringType!);
        var access = Expression.Property(typed, property);
        var box = Expression.Convert(access, typeof(object));
        return Expression.Lambda<Func<object, object?>>(box, instance).Compile();
    }
}
