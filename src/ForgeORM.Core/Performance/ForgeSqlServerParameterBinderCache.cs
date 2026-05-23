using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

internal static class ForgeSqlServerParameterBinderCache
{
    private static readonly ConcurrentDictionary<SqlServerParameterBinderKey, Action<SqlCommand, object>> Cache = new();

    public static Action<SqlCommand, object> GetOrAdd(Type parameterType, string[] parameterNames)
        => GetOrAdd(parameterType, parameterNames, CreateParameterNamesKey(parameterNames));

    public static Action<SqlCommand, object> GetOrAdd(Type parameterType, string[] parameterNames, string parameterNamesKey)
    {
        var key = new SqlServerParameterBinderKey(parameterType, parameterNamesKey);
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        var built = Build(parameterType, parameterNames);
        return Cache.GetOrAdd(key, built);
    }

    private static string CreateParameterNamesKey(string[] parameterNames)
    {
        if (parameterNames.Length == 0) return string.Empty;
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
            EmitBindProperty(il, typed, prop, name);
        }

        il.Emit(OpCodes.Ret);
        return (Action<SqlCommand, object>)method.CreateDelegate(typeof(Action<SqlCommand, object>));
    }


    private static void EmitBindProperty(ILGenerator il, LocalBuilder typed, PropertyInfo prop, string parameterName)
    {
        var propertyType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        MethodInfo? fastMethod = propertyType == typeof(int)
            ? typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddInt32Parameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            : propertyType == typeof(long)
                ? typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddInt64Parameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                : propertyType == typeof(string)
                    ? typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddStringParameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    : propertyType == typeof(decimal)
                        ? typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddDecimalParameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                        : propertyType == typeof(Guid)
                            ? typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddGuidParameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                            : propertyType == typeof(bool)
                                ? typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddBooleanParameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                                : propertyType == typeof(DateTime)
                                    ? typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddDateTimeParameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                                    : null;

        if (fastMethod is not null && Nullable.GetUnderlyingType(prop.PropertyType) is null)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, parameterName);
            il.Emit(OpCodes.Ldloc, typed);
            il.EmitCall(prop.GetMethod!.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, prop.GetMethod!, null);
            il.Emit(OpCodes.Call, fastMethod);
            return;
        }

        if (propertyType.IsEnum && Nullable.GetUnderlyingType(prop.PropertyType) is null)
        {
            var enumMethod = typeof(ForgeSqlServerProviderDirectHotPath)
                .GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddEnumInt32Parameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!
                .MakeGenericMethod(propertyType);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, parameterName);
            il.Emit(OpCodes.Ldloc, typed);
            il.EmitCall(prop.GetMethod!.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, prop.GetMethod!, null);
            il.Emit(OpCodes.Call, enumMethod);
            return;
        }

        var add = typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddTypedParameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldstr, parameterName);
        il.Emit(OpCodes.Ldloc, typed);
        il.EmitCall(prop.GetMethod!.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, prop.GetMethod!, null);
        if (prop.PropertyType.IsValueType) il.Emit(OpCodes.Box, prop.PropertyType);
        il.Emit(OpCodes.Ldtoken, prop.PropertyType);
        il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
        il.Emit(OpCodes.Call, add);
    }

    private readonly record struct SqlServerParameterBinderKey(Type ParameterType, string Names);
}
