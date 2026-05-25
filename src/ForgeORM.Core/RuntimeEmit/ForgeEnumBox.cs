using System.Collections.Concurrent;
using System.Reflection.Emit;

namespace ForgeORM.Core;

internal static class ForgeEnumBox
{
    private static readonly ConcurrentDictionary<Type, Func<object, object>> Cache = new();

    public static object ToUnderlying(object value)
        => Cache.GetOrAdd(value.GetType(), Build)(value);

    private static Func<object, object> Build(Type enumType)
    {
        var underlying = Enum.GetUnderlyingType(enumType);
        var method = new DynamicMethod($"ForgeORM_EnumUnderlying_{enumType.Name}", typeof(object), new[] { typeof(object) }, typeof(ForgeEnumBox).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Unbox_Any, enumType);
        il.Emit(OpCodes.Box, underlying);
        il.Emit(OpCodes.Ret);
        return (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
    }
}
