using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Core;

/// <summary>
/// RuntimeEmit hot-path accessor cache. Reflection is used only while building the cached delegate;
/// execution uses MSIL DynamicMethod delegates from ConcurrentDictionary caches.
/// </summary>
public static class ForgeRuntimeAccessorCache
{
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> Getters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> Setters = new();
    private static readonly ConcurrentDictionary<Type, Func<object>> Constructors = new();
    private static readonly ConcurrentDictionary<Type, object?> Defaults = new();

    public static Func<object, object?> Getter(PropertyInfo property)
        => Getters.GetOrAdd(property, BuildGetter);

    public static Action<object, object?> Setter(PropertyInfo property)
        => Setters.GetOrAdd(property, BuildSetter);

    public static Func<object> Constructor(Type type)
        => Constructors.GetOrAdd(type, BuildConstructor);

    public static object? DefaultValue(Type type)
    {
        var actual = Nullable.GetUnderlyingType(type) ?? type;
        if (!actual.IsValueType)
            return null;

        return Defaults.GetOrAdd(actual, static t => Constructor(t)());
    }

    public static object? Get(PropertyInfo property, object instance)
        => Getter(property)(instance);

    public static void Set(PropertyInfo property, object instance, object? value)
        => Setter(property)(instance, value);

    private static Func<object, object?> BuildGetter(PropertyInfo property)
    {
        var method = new DynamicMethod(
            $"ForgeORM_Get_{Sanitize(property.DeclaringType?.FullName ?? "Type")}_{property.Name}",
            typeof(object),
            new[] { typeof(object) },
            typeof(ForgeRuntimeAccessorCache).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();
        var declaringType = property.DeclaringType ?? throw new InvalidOperationException($"Property '{property.Name}' has no declaring type.");
        var getter = property.GetMethod ?? throw new InvalidOperationException($"Property '{property.Name}' has no getter.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
        il.EmitCall(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter, null);
        if (property.PropertyType.IsValueType)
            il.Emit(OpCodes.Box, property.PropertyType);
        il.Emit(OpCodes.Ret);

        return (Func<object, object?>)method.CreateDelegate(typeof(Func<object, object?>));
    }

    private static Action<object, object?> BuildSetter(PropertyInfo property)
    {
        var method = new DynamicMethod(
            $"ForgeORM_Set_{Sanitize(property.DeclaringType?.FullName ?? "Type")}_{property.Name}",
            typeof(void),
            new[] { typeof(object), typeof(object) },
            typeof(ForgeRuntimeAccessorCache).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();
        var declaringType = property.DeclaringType ?? throw new InvalidOperationException($"Property '{property.Name}' has no declaring type.");
        var setter = property.SetMethod ?? throw new InvalidOperationException($"Property '{property.Name}' has no setter.");

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
        il.Emit(OpCodes.Ldarg_1);
        EmitCastOrUnbox(il, property.PropertyType);
        il.EmitCall(setter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setter, null);
        il.Emit(OpCodes.Ret);

        return (Action<object, object?>)method.CreateDelegate(typeof(Action<object, object?>));
    }

    private static Func<object> BuildConstructor(Type type)
    {
        var method = new DynamicMethod(
            $"ForgeORM_New_{Sanitize(type.FullName ?? type.Name)}",
            typeof(object),
            Type.EmptyTypes,
            typeof(ForgeRuntimeAccessorCache).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();
        if (type.IsValueType)
        {
            var local = il.DeclareLocal(type);
            il.Emit(OpCodes.Ldloca_S, local);
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Ldloc, local);
            il.Emit(OpCodes.Box, type);
        }
        else
        {
            var ctor = type.GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException($"{type.FullName} must have a parameterless constructor.");
            il.Emit(OpCodes.Newobj, ctor);
        }

        il.Emit(OpCodes.Ret);
        return (Func<object>)method.CreateDelegate(typeof(Func<object>));
    }

    private static void EmitCastOrUnbox(ILGenerator il, Type targetType)
    {
        var nullable = Nullable.GetUnderlyingType(targetType);
        if (nullable is not null)
        {
            il.Emit(OpCodes.Unbox_Any, targetType);
            return;
        }

        if (targetType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, targetType);
        else
            il.Emit(OpCodes.Castclass, targetType);
    }

    private static string Sanitize(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
        return new string(chars);
    }
}
