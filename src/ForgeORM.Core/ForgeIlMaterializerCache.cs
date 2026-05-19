using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Core;

/// <summary>
/// Dapper-style MSIL materializer cache.
/// One DynamicMethod is emitted per entity type + result column shape and reused for every row.
/// No PropertyInfo.SetValue, no Activator hot path, no per-row reflection.
/// Enums are stored/read as their numeric underlying type.
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
        // Built once per execution, then hits ConcurrentDictionary for repeated query shapes.
        // Include field types to avoid reusing a plan where same names have different DB types.
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
            name: $"ForgeORM_Materialize_{SanitizeName(actualType.FullName ?? actualType.Name)}_{Guid.NewGuid():N}",
            returnType: targetType,
            parameterTypes: new[] { typeof(DbDataReader) },
            m: typeof(ForgeIlMaterializerCache).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();

        if (ForgeMaterializer.IsScalar(actualType))
        {
            EmitScalarReturn<T>(il, targetType, actualType);
            return (Func<DbDataReader, T>)method.CreateDelegate(typeof(Func<DbDataReader, T>));
        }

        var ctor = actualType.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"{actualType.FullName} must have a parameterless constructor for MSIL materialization.");

        var entity = il.DeclareLocal(actualType);
        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Stloc, entity);

        var properties = actualType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && Support.IsScalarColumn(p))
            .ToDictionary(
                p => p.GetCustomAttribute<ForgeORM.Abstractions.ForgeColumnAttribute>()?.Name ?? p.Name,
                p => p,
                StringComparer.OrdinalIgnoreCase);

        var isDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull), new[] { typeof(int) })!;

        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            var columnName = reader.GetName(ordinal);
            if (!properties.TryGetValue(columnName, out var property))
                continue;

            var setter = property.SetMethod;
            if (setter is null)
                continue;

            var endLabel = il.DefineLabel();

            // if (reader.IsDBNull(ordinal)) goto end;
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ordinal);
            il.Emit(OpCodes.Callvirt, isDbNull);
            il.Emit(OpCodes.Brtrue_S, endLabel);

            // entity.Property = reader.GetFieldValue<TColumn>(ordinal)
            il.Emit(OpCodes.Ldloc, entity);
            EmitReadValueForProperty(il, property.PropertyType, ordinal);
            il.Emit(OpCodes.Callvirt, setter);

            il.MarkLabel(endLabel);
        }

        il.Emit(OpCodes.Ldloc, entity);
        il.Emit(OpCodes.Ret);

        return (Func<DbDataReader, T>)method.CreateDelegate(typeof(Func<DbDataReader, T>));
    }

    private static void EmitScalarReturn<T>(ILGenerator il, Type targetType, Type actualType)
    {
        var isDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull), new[] { typeof(int) })!;
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
        EmitReadValue(il, targetType, actualType, 0);
        il.Emit(OpCodes.Stloc, result);

        il.MarkLabel(end);
        il.Emit(OpCodes.Ldloc, result);
        il.Emit(OpCodes.Ret);
    }

    private static void EmitReadValueForProperty(ILGenerator il, Type propertyType, int ordinal)
    {
        var underlyingNullable = Nullable.GetUnderlyingType(propertyType);
        var valueType = underlyingNullable ?? propertyType;

        EmitReadValue(il, propertyType, valueType, ordinal);
    }

    private static void EmitReadValue(ILGenerator il, Type finalType, Type valueType, int ordinal)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(finalType);
        var readType = valueType.IsEnum ? Enum.GetUnderlyingType(valueType) : valueType;

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, ordinal);

        var getFieldValue = typeof(DbDataReader)
            .GetMethod(nameof(DbDataReader.GetFieldValue))!
            .MakeGenericMethod(readType);

        il.Emit(OpCodes.Callvirt, getFieldValue);

        // Enum values are represented by their underlying numeric type on the evaluation stack.
        // The setter accepts the enum type but the stack representation is compatible.
        if (nullableUnderlying is not null)
        {
            var nullableValueType = nullableUnderlying.IsEnum
                ? Enum.GetUnderlyingType(nullableUnderlying)
                : nullableUnderlying;

            if (nullableUnderlying.IsEnum && nullableValueType != nullableUnderlying)
            {
                // Numeric stack value is valid for enum nullable constructor.
            }

            var ctor = finalType.GetConstructor(new[] { nullableUnderlying })!;
            il.Emit(OpCodes.Newobj, ctor);
        }
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
