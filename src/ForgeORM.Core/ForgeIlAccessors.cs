using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Core;

/// <summary>
/// Central zero-reflection hot-path accessor cache.
/// Reflection is used only once while building the cached plan; every execution path uses MSIL delegates.
/// </summary>
internal static class ForgeIlAccessors
{
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> GetterCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> SetterCache = new();
    private static readonly ConcurrentDictionary<Type, Func<object>> ConstructorCache = new();
    private static readonly ConcurrentDictionary<Type, Func<IList>> ListFactoryCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyAccessorPlan> PlanCache = new();
    private static readonly ConcurrentDictionary<Type, object?> DefaultValueCache = new();

    public static PropertyAccessorPlan For(Type type) => PlanCache.GetOrAdd(type, BuildPlan);

    public static Func<object, object?> Getter(PropertyInfo property) => GetterCache.GetOrAdd(property, BuildGetter);

    public static Action<object, object?> Setter(PropertyInfo property) => SetterCache.GetOrAdd(property, BuildSetter);

    public static object? Get(PropertyInfo property, object instance) => Getter(property)(instance);

    public static void Set(PropertyInfo property, object instance, object? value)
        => Setter(property)(instance, ForgeObjectMapper.ConvertTo(value, property.PropertyType));

    public static object Create(Type type) => ConstructorCache.GetOrAdd(type, BuildConstructor)();

    public static IList CreateList(Type itemType) => ListFactoryCache.GetOrAdd(itemType, BuildListFactory)();

    public static object? DefaultValue(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        if (!actual.IsValueType)
            return null;
        return DefaultValueCache.GetOrAdd(actual, CreateDefaultValue);
    }

    private static PropertyAccessorPlan BuildPlan(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var accessors = properties
            .Select(p => new ForgePropertyAccessor(
                p,
                p.Name,
                p.PropertyType,
                p.CanRead ? Getter(p) : null,
                p.CanWrite ? Setter(p) : null))
            .ToArray();

        return new PropertyAccessorPlan(
            type,
            accessors,
            accessors.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase));
    }

    private static Func<object, object?> BuildGetter(PropertyInfo property)
    {
        if (property.GetMethod is null)
            return _ => null;

        var method = new DynamicMethod(
            $"ForgeORM_Get_{Sanitize(property.DeclaringType?.FullName ?? "Type")}_{Sanitize(property.Name)}",
            typeof(object),
            new[] { typeof(object) },
            typeof(ForgeIlAccessors).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(property.DeclaringType!.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, property.DeclaringType!);
        il.Emit(OpCodes.Callvirt, property.GetMethod);
        if (property.PropertyType.IsValueType)
            il.Emit(OpCodes.Box, property.PropertyType);
        il.Emit(OpCodes.Ret);

        return (Func<object, object?>)method.CreateDelegate(typeof(Func<object, object?>));
    }

    private static Action<object, object?> BuildSetter(PropertyInfo property)
    {
        if (property.SetMethod is null)
            return (_, _) => { };

        var method = new DynamicMethod(
            $"ForgeORM_Set_{Sanitize(property.DeclaringType?.FullName ?? "Type")}_{Sanitize(property.Name)}",
            typeof(void),
            new[] { typeof(object), typeof(object) },
            typeof(ForgeIlAccessors).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(property.DeclaringType!.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, property.DeclaringType!);
        il.Emit(OpCodes.Ldarg_1);

        var targetType = property.PropertyType;
        if (targetType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, targetType);
        else
            il.Emit(OpCodes.Castclass, targetType);

        il.Emit(OpCodes.Callvirt, property.SetMethod);
        il.Emit(OpCodes.Ret);

        return (Action<object, object?>)method.CreateDelegate(typeof(Action<object, object?>));
    }

    private static Func<object> BuildConstructor(Type type)
    {
        var ctor = type.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"{type.FullName} must have a parameterless constructor for ForgeORM zero-reflection hot path.");

        var method = new DynamicMethod(
            $"ForgeORM_New_{Sanitize(type.FullName ?? type.Name)}",
            typeof(object),
            Type.EmptyTypes,
            typeof(ForgeIlAccessors).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Newobj, ctor);
        if (type.IsValueType)
            il.Emit(OpCodes.Box, type);
        il.Emit(OpCodes.Ret);

        return (Func<object>)method.CreateDelegate(typeof(Func<object>));
    }

    private static Func<IList> BuildListFactory(Type itemType)
    {
        var listType = typeof(List<>).MakeGenericType(itemType);
        var ctor = listType.GetConstructor(Type.EmptyTypes)!;
        var method = new DynamicMethod(
            $"ForgeORM_NewList_{Sanitize(itemType.FullName ?? itemType.Name)}",
            typeof(IList),
            Type.EmptyTypes,
            typeof(ForgeIlAccessors).Module,
            skipVisibility: true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Ret);
        return (Func<IList>)method.CreateDelegate(typeof(Func<IList>));
    }

    private static object? CreateDefaultValue(Type type)
    {
        var method = new DynamicMethod(
            $"ForgeORM_Default_{Sanitize(type.FullName ?? type.Name)}",
            typeof(object),
            Type.EmptyTypes,
            typeof(ForgeIlAccessors).Module,
            skipVisibility: true);
        var il = method.GetILGenerator();
        var local = il.DeclareLocal(type);
        il.Emit(OpCodes.Ldloca_S, local);
        il.Emit(OpCodes.Initobj, type);
        il.Emit(OpCodes.Ldloc, local);
        il.Emit(OpCodes.Box, type);
        il.Emit(OpCodes.Ret);
        var factory = (Func<object?>)method.CreateDelegate(typeof(Func<object?>));
        return factory();
    }

    private static string Sanitize(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
        return new string(chars);
    }
}
