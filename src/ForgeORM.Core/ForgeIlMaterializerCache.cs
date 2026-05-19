using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Core;

/// <summary>
/// High-performance MSIL materializer cache.
/// Builds one DynamicMethod per result-shape and entity type, then reuses it for every row.
/// This avoids reflection SetValue / Activator hot-path cost and moves ForgeORM closer to Dapper-style materialization.
/// </summary>
internal static class ForgeIlMaterializerCache
{
    private static readonly ConcurrentDictionary<string, Delegate> Cache = new(StringComparer.Ordinal);

    public static Func<DbDataReader, T> GetOrCreate<T>(DbDataReader reader)
    {
        var key = CreateKey<T>(reader);
        return (Func<DbDataReader, T>)Cache.GetOrAdd(key, _ => CreateMaterializer<T>(reader));
    }

    private static string CreateKey<T>(DbDataReader reader)
    {
        var parts = new string[(reader.FieldCount * 2) + 1];
        parts[0] = typeof(T).FullName ?? typeof(T).Name;

        var index = 1;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            parts[index++] = reader.GetName(i);
            parts[index++] = reader.GetFieldType(i).FullName ?? reader.GetFieldType(i).Name;
        }

        return string.Join('|', parts);
    }

    private static Func<DbDataReader, T> CreateMaterializer<T>(DbDataReader reader)
    {
        var targetType = typeof(T);
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        var method = new DynamicMethod(
            name: $"ForgeORM_MSIL_Materialize_{SanitizeName(actualType.FullName ?? actualType.Name)}_{Guid.NewGuid():N}",
            returnType: targetType,
            parameterTypes: new[] { typeof(DbDataReader) },
            m: typeof(ForgeIlMaterializerCache).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();

        if (ForgeMaterializer.IsScalar(actualType))
        {
            EmitScalarReturn<T>(il, actualType);
            return (Func<DbDataReader, T>)method.CreateDelegate(typeof(Func<DbDataReader, T>));
        }

        var ctor = actualType.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"{actualType.FullName} must have a parameterless constructor for MSIL materialization.");

        var entity = il.DeclareLocal(actualType);

        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Stloc, entity);

        var properties = actualType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && ForgeMaterializer.IsScalar(p.PropertyType))
            .ToDictionary(
                p => p.GetCustomAttribute<ForgeORM.Abstractions.ForgeColumnAttribute>()?.Name ?? p.Name,
                p => p,
                StringComparer.OrdinalIgnoreCase);

        var isDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull), new[] { typeof(int) })!;
        var getValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetValue), new[] { typeof(int) })!;
        var fromDatabase = typeof(ForgeValueConverter).GetMethod(
            nameof(ForgeValueConverter.FromDatabase),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(object), typeof(Type) },
            modifiers: null)!;
        var getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) })!;

        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            var columnName = reader.GetName(ordinal);
            if (!properties.TryGetValue(columnName, out var property))
                continue;

            var setter = property.SetMethod;
            if (setter is null)
                continue;

            var endLabel = il.DefineLabel();
            var converted = il.DeclareLocal(typeof(object));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ordinal);
            il.Emit(OpCodes.Callvirt, isDbNull);
            il.Emit(OpCodes.Brtrue_S, endLabel);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ordinal);
            il.Emit(OpCodes.Callvirt, getValue);
            il.Emit(OpCodes.Ldtoken, property.PropertyType);
            il.Emit(OpCodes.Call, getTypeFromHandle);
            il.Emit(OpCodes.Call, fromDatabase);
            il.Emit(OpCodes.Stloc, converted);

            il.Emit(OpCodes.Ldloc, entity);
            il.Emit(OpCodes.Ldloc, converted);
            EmitCastOrUnbox(il, property.PropertyType);
            il.Emit(OpCodes.Callvirt, setter);

            il.MarkLabel(endLabel);
        }

        il.Emit(OpCodes.Ldloc, entity);
        il.Emit(OpCodes.Ret);

        return (Func<DbDataReader, T>)method.CreateDelegate(typeof(Func<DbDataReader, T>));
    }

    private static void EmitScalarReturn<T>(ILGenerator il, Type actualType)
    {
        var targetType = typeof(T);
        var isDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull), new[] { typeof(int) })!;
        var getValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetValue), new[] { typeof(int) })!;
        var fromDatabaseGeneric = typeof(ForgeValueConverter)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(x => x.Name == nameof(ForgeValueConverter.FromDatabase)
                        && x.IsGenericMethodDefinition
                        && x.GetParameters().Length == 1)
            .MakeGenericMethod(targetType);

        var notNull = il.DefineLabel();
        var end = il.DefineLabel();
        var result = il.DeclareLocal(targetType);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Callvirt, isDbNull);
        il.Emit(OpCodes.Brfalse_S, notNull);

        il.Emit(OpCodes.Ldloca_S, result);
        il.Emit(OpCodes.Initobj, targetType);
        il.Emit(OpCodes.Br_S, end);

        il.MarkLabel(notNull);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Callvirt, getValue);
        il.Emit(OpCodes.Call, fromDatabaseGeneric);
        il.Emit(OpCodes.Stloc, result);

        il.MarkLabel(end);
        il.Emit(OpCodes.Ldloc, result);
        il.Emit(OpCodes.Ret);
    }

    private static void EmitCastOrUnbox(ILGenerator il, Type targetType)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(targetType);
        if (nullableUnderlying is not null)
        {
            il.Emit(OpCodes.Unbox_Any, nullableUnderlying);
            var ctor = targetType.GetConstructor(new[] { nullableUnderlying })!;
            il.Emit(OpCodes.Newobj, ctor);
            return;
        }

        if (targetType.IsValueType)
        {
            il.Emit(OpCodes.Unbox_Any, targetType);
            return;
        }

        il.Emit(OpCodes.Castclass, targetType);
    }

    private static string SanitizeName(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }

        return new string(chars);
    }
}
