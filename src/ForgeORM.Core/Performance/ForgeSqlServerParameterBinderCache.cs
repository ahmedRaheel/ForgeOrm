using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

internal static class ForgeSqlServerParameterBinderCache
{
    private static readonly ConcurrentDictionary<SqlServerParameterBinderKey, Action<SqlCommand, object>> Cache = new();

    public static Action<SqlCommand, object> GetOrAdd(Type parameterType, string[] parameterNames)
        => Cache.GetOrAdd(new SqlServerParameterBinderKey(parameterType, FingerprintNames(parameterNames)),
            _ => Build(parameterType, parameterNames));

    private static ulong FingerprintNames(string[] names)
    {
        unchecked
        {
            var hash = 1469598103934665603UL;
            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i].AsSpan();
                for (var c = 0; c < name.Length; c++)
                {
                    var ch = name[c];
                    if (ch == '@' || ch == ':') continue;
                    hash ^= char.ToUpperInvariant(ch);
                    hash *= 1099511628211UL;
                }
                hash ^= 0x9E3779B97F4A7C15UL;
                hash *= 1099511628211UL;
            }
            return hash;
        }
    }

    private static Action<SqlCommand, object> Build(Type parameterType, string[] parameterNames)
    {
        var allProperties = parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var selected = new List<PropertyInfo>(parameterNames.Length == 0 ? allProperties.Length : parameterNames.Length);

        if (parameterNames.Length == 0)
        {
            for (var i = 0; i < allProperties.Length; i++)
            {
                var p = allProperties[i];
                if (p.CanRead && p.GetIndexParameters().Length == 0)
                    selected.Add(p);
            }
        }
        else
        {
            for (var n = 0; n < parameterNames.Length; n++)
            {
                var clean = parameterNames[n].AsSpan().TrimStart('@', ':');
                for (var i = 0; i < allProperties.Length; i++)
                {
                    var p = allProperties[i];
                    if (!p.CanRead || p.GetIndexParameters().Length != 0) continue;
                    if (!clean.Equals(p.Name.AsSpan(), StringComparison.OrdinalIgnoreCase)) continue;
                    selected.Add(p);
                    break;
                }
            }
        }

        var method = new DynamicMethod(
            $"ForgeORM_SqlServerBind_{Sanitize(parameterType.FullName ?? parameterType.Name)}",
            typeof(void),
            new[] { typeof(SqlCommand), typeof(object) },
            typeof(ForgeSqlServerParameterBinderCache).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();
        var typed = il.DeclareLocal(parameterType);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(parameterType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, parameterType);
        il.Emit(OpCodes.Stloc, typed);

        var add = typeof(ForgeSqlServerProviderDirectHotPath).GetMethod(nameof(ForgeSqlServerProviderDirectHotPath.AddTypedParameter), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;

        for (var i = 0; i < selected.Count; i++)
        {
            var prop = selected[i];
            var parameterName = FindParameterName(parameterNames, prop.Name) ?? "@" + prop.Name;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, parameterName);
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

    private static string? FindParameterName(string[] names, string propertyName)
    {
        for (var i = 0; i < names.Length; i++)
        {
            var clean = names[i].AsSpan().TrimStart('@', ':');
            if (clean.Equals(propertyName.AsSpan(), StringComparison.OrdinalIgnoreCase))
                return names[i];
        }
        return null;
    }

    private static string Sanitize(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
        return new string(chars);
    }

    private readonly record struct SqlServerParameterBinderKey(Type ParameterType, ulong NamesFingerprint);
}
