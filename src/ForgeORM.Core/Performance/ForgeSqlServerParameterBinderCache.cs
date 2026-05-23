using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

internal static class ForgeSqlServerParameterBinderCache
{
    private static readonly ConcurrentDictionary<SqlServerParameterBinderKey, Action<SqlCommand, object>> Cache = new();

    public static Action<SqlCommand, object> GetOrAdd(Type parameterType, string[] parameterNames)
        => GetOrAdd(parameterType, parameterNames, CreateParameterNameKey(parameterNames));

    public static Action<SqlCommand, object> GetOrAdd(Type parameterType, string[] parameterNames, string parameterNameKey)
        => Cache.GetOrAdd(new SqlServerParameterBinderKey(parameterType, parameterNameKey),
            _ => Build(parameterType, parameterNames));

    private static string CreateParameterNameKey(string[] parameterNames)
    {
        if (parameterNames.Length == 0)
            return string.Empty;
        var copy = new string[parameterNames.Length];
        Array.Copy(parameterNames, copy, parameterNames.Length);
        Array.Sort(copy, StringComparer.OrdinalIgnoreCase);
        return string.Join("|", copy);
    }

    private static Action<SqlCommand, object> Build(Type parameterType, string[] parameterNames)
    {
        var properties = parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        var method = new DynamicMethod(
            $"ForgeORM_SqlServerBind_{parameterType.Name}_{Guid.NewGuid():N}",
            typeof(void),
            new[] { typeof(SqlCommand), typeof(object) },
            typeof(ForgeSqlServerParameterBinderCache).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();
        var typed = il.DeclareLocal(parameterType);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(parameterType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, parameterType);
        il.Emit(OpCodes.Stloc, typed);

        foreach (var name in parameterNames)
        {
            var clean = name.TrimStart('@');
            if (!properties.TryGetValue(clean, out var prop))
                continue;
            var add = typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddTypedParameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, name);
            il.Emit(OpCodes.Ldloc, typed);
            il.EmitCall(prop.GetMethod!.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, prop.GetMethod!, null);
            if (prop.PropertyType.IsValueType) il.Emit(OpCodes.Box, prop.PropertyType);
            il.Emit(OpCodes.Ldtoken, prop.PropertyType);
            il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
            il.Emit(OpCodes.Call, add);
        }

        il.Emit(OpCodes.Ret);
        return (Action<SqlCommand, object>)method.CreateDelegate(typeof(Action<SqlCommand, object>));
    }

    private readonly record struct SqlServerParameterBinderKey(Type ParameterType, string Names);
}
