using ForgeORM.Abstractions;
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
    private static readonly ConcurrentDictionary<string, Delegate> ObjectCache = new(StringComparer.Ordinal);

    public static Func<DbDataReader, T> GetOrCreate<T>(DbDataReader reader)
    {
        if (ForgeGeneratedRegistry.TryGetReader<T>(out var generated))
            return generated;

        var key = CreateKey(typeof(T), reader);
        return (Func<DbDataReader, T>)Cache.GetOrAdd(key, _ => CreateMaterializer<T>(reader));
    }

    public static Func<DbDataReader, object> GetOrCreate(Type type, DbDataReader reader)
    {
        var key = CreateKey(type, reader);
        return (Func<DbDataReader, object>)ObjectCache.GetOrAdd(key, _ => CreateObjectMaterializer(type, reader));
    }

    private static string CreateKey(Type type, DbDataReader reader)
    {
        // Built once per execution, then hits ConcurrentDictionary for repeated query shapes.
        // Include field types to avoid reusing a plan where same names have different DB types.
        var parts = new string[(reader.FieldCount * 2) + 1];
        parts[0] = type.FullName ?? type.Name;

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


    private static Func<DbDataReader, object> CreateObjectMaterializer(Type runtimeType, DbDataReader reader)
    {
        var method = new DynamicMethod(
            name: $"ForgeORM_Materialize_Object_{SanitizeName(runtimeType.FullName ?? runtimeType.Name)}_{Guid.NewGuid():N}",
            returnType: typeof(object),
            parameterTypes: new[] { typeof(DbDataReader) },
            m: typeof(ForgeIlMaterializerCache).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();

        if (ForgeMaterializer.IsScalar(runtimeType))
        {
            EmitReadValue(il, typeof(object), runtimeType, 0);
            if (runtimeType.IsValueType)
                il.Emit(OpCodes.Box, runtimeType);
            il.Emit(OpCodes.Ret);
            return (Func<DbDataReader, object>)method.CreateDelegate(typeof(Func<DbDataReader, object>));
        }

        var ctor = runtimeType.GetConstructor(Type.EmptyTypes)
            ?? throw new InvalidOperationException($"{runtimeType.FullName} must have a parameterless constructor for MSIL materialization.");

        var entity = il.DeclareLocal(runtimeType);
        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Stloc, entity);

        var properties = runtimeType
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
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ordinal);
            il.Emit(OpCodes.Callvirt, isDbNull);
            il.Emit(OpCodes.Brtrue_S, endLabel);

            il.Emit(OpCodes.Ldloc, entity);
            EmitReadValueForProperty(il, property.PropertyType, ordinal);
            il.Emit(OpCodes.Callvirt, setter);
            il.MarkLabel(endLabel);
        }

        il.Emit(OpCodes.Ldloc, entity);
        if (runtimeType.IsValueType)
            il.Emit(OpCodes.Box, runtimeType);
        il.Emit(OpCodes.Ret);
        return (Func<DbDataReader, object>)method.CreateDelegate(typeof(Func<DbDataReader, object>));
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
    internal static class Support
    {
        public static bool IsScalarColumn(PropertyInfo property)
        {
            return IsScalarColumnType(property.PropertyType);
        }

        public static bool IsScalarColumnType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type.IsPrimitive
                || type.IsEnum
                || type == typeof(string)
                || type == typeof(Guid)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(DateOnly)
                || type == typeof(TimeOnly)
                || type == typeof(TimeSpan)
                || type == typeof(byte[]);
        }

        public static bool IsCollectionNavigation(PropertyInfo property)
        {
            if (property.PropertyType == typeof(string))
                return false;

            return property.PropertyType.IsGenericType
                && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>);
        }

        public static bool IsReferenceNavigation(PropertyInfo property)
        {
            if (property.PropertyType == typeof(string))
                return false;

            if (IsCollectionNavigation(property))
                return false;

            var type = Nullable.GetUnderlyingType(property.PropertyType)
                       ?? property.PropertyType;

            return type.IsClass && !IsScalarColumnType(type);
        }

        public static string ResolveTableName(Type type)
        {
            return type.GetCustomAttribute<ForgeTableAttribute>()?.Name
                ?? type.Name;
        }

        public static string ResolveScalarColumns(Type type)
        {
            var columns = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(IsScalarColumn)
                .Select(x => x.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? x.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return columns.Length == 0
                ? "*"
                : string.Join(", ", columns);
        }

        public static PropertyInfo[] GetScalarProperties(
            Type type,
            bool includeIdentity = false)
        {
            return type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead)
                .Where(IsScalarColumn)
                .Where(x => includeIdentity || !IsIdentityColumn(x))
                .ToArray();
        }

        public static bool IsIdentityColumn(PropertyInfo property)
        {
            return property.Name.Equals(
                       "Id",
                       StringComparison.OrdinalIgnoreCase)
                   || property.GetCustomAttributes()
                       .Any(x => x.GetType().Name.Contains(
                           "Key",
                           StringComparison.OrdinalIgnoreCase));
        }

        public static object? NormalizeParameterValue(object? value)
        {
            if (value is null)
                return null;

            if (value is Enum enumValue)
            {
                return Convert.ChangeType(
                    enumValue,
                    Enum.GetUnderlyingType(enumValue.GetType()));
            }

            if (value is DateTime dateTime)
            {
                if (dateTime == default ||
                    dateTime < new DateTime(1753, 1, 1))
                {
                    return DateTime.UtcNow;
                }

                return dateTime;
            }

            if (value is DateTimeOffset dateTimeOffset)
            {
                if (dateTimeOffset == default)
                    return DateTimeOffset.UtcNow;

                return dateTimeOffset;
            }

            return value;
        }

        public static (int Skip, int Take) NormalizePaging(
            int skip,
            int take)
        {
            if (skip < 0)
                skip = 0;

            if (take <= 0)
                take = 1;

            if (skip == take)
                take++;

            return (skip, take);
        }

        public static void ResetIdentityValue(
            object entity,
            PropertyInfo? identityProperty)
        {
            if (identityProperty is null)
                return;

            var type = Nullable.GetUnderlyingType(identityProperty.PropertyType)
                       ?? identityProperty.PropertyType;

            object? value = type.IsValueType
                ? Activator.CreateInstance(type)
                : null;

            identityProperty.SetValue(entity, value);
        }
    }
}
