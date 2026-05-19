using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Providers.SqlServer;

internal static class ForgeProviderAccessors
{
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> Getters = new();

    public static object? Get(PropertyInfo? property, object instance)
    {
        if (property is null) return null;
        return Getters.GetOrAdd(property, BuildGetter)(instance);
    }

    private static Func<object, object?> BuildGetter(PropertyInfo property)
    {
        var method = new DynamicMethod($"ForgeORM_ProviderGet_{property.Name}", typeof(object), new[] { typeof(object) }, typeof(ForgeProviderAccessors).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(property.DeclaringType!.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, property.DeclaringType!);
        il.EmitCall(property.GetMethod!.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, property.GetMethod!, null);
        if (property.PropertyType.IsValueType) il.Emit(OpCodes.Box, property.PropertyType);
        il.Emit(OpCodes.Ret);
        return (Func<object, object?>)method.CreateDelegate(typeof(Func<object, object?>));
    }
}
