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
        var type = typeof(T);
        var mode = ForgeSourceGeneratedRegistry.CompilationMode;

        if (ForgeSourceGeneratedRegistry.ShouldUseSourceGenerated &&
            ForgeSourceGeneratedRegistry.TryGetProvider(type, out var provider) &&
            provider.TryCreateReader<T>(reader, out var sourceReader) &&
            sourceReader is not null)
        {
            return sourceReader;
        }

        if (mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            throw new InvalidOperationException($"No ForgeORM source-generated reader was registered for {type.FullName}.");

        var key = mode + "|" + ForgeReaderShapeCache.CreateKey(type, reader);
        return (Func<DbDataReader, T>)Cache.GetOrAdd(key, _ => CreateMaterializer<T>(reader));
    }

    public static Func<DbDataReader, object> GetOrCreate(Type type, DbDataReader reader)
    {
        var mode = ForgeSourceGeneratedRegistry.CompilationMode;

        if (ForgeSourceGeneratedRegistry.ShouldUseSourceGenerated &&
            ForgeSourceGeneratedRegistry.TryGetProvider(type, out var provider))
        {
            return provider.GetReader(type, reader);
        }

        if (mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            throw new InvalidOperationException($"No ForgeORM source-generated reader was registered for {type.FullName}.");

        var key = mode + "|" + ForgeReaderShapeCache.CreateKey(type, reader);
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

        var constructorPlan = CreateConstructorPlan(actualType, reader);
        var entity = il.DeclareLocal(actualType);

        if (constructorPlan.Constructor is not null && constructorPlan.Parameters.Length > 0)
        {
            EmitConstructorArguments(il, constructorPlan, reader);
            il.Emit(OpCodes.Newobj, constructorPlan.Constructor);
            il.Emit(OpCodes.Stloc, entity);
        }
        else
        {
            var ctor = actualType.GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException($"{actualType.FullName} must have either a parameterless constructor or a constructor whose parameter names match result columns for MSIL/record materialization.");

            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc, entity);
        }

        var constructorParameterNames = constructorPlan.ParameterNames;
        var properties = actualType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && Support.IsScalarColumn(p))
            .Where(p => !constructorParameterNames.Contains(p.Name))
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

        // Never call GetFieldValue<TEnum>() directly. SQL may store enums as strings or ints.
        // Route enum materialization through ForgeEnumReader so records and classes behave the same.
        if (valueType.IsEnum)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ordinal);

            if (nullableUnderlying is not null)
            {
                var nullableEnumReader = typeof(ForgeEnumReader)
                    .GetMethod(nameof(ForgeEnumReader.ReadNullableEnum))!
                    .MakeGenericMethod(valueType);
                il.Emit(OpCodes.Call, nullableEnumReader);
            }
            else
            {
                var enumReader = typeof(ForgeEnumReader)
                    .GetMethod(nameof(ForgeEnumReader.ReadEnum))!
                    .MakeGenericMethod(valueType);
                il.Emit(OpCodes.Call, enumReader);
            }

            return;
        }

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, ordinal);

        var getFieldValue = typeof(DbDataReader)
            .GetMethod(nameof(DbDataReader.GetFieldValue))!
            .MakeGenericMethod(valueType);

        il.Emit(OpCodes.Callvirt, getFieldValue);

        if (nullableUnderlying is not null)
        {
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

        var constructorPlan = CreateConstructorPlan(runtimeType, reader);
        var entity = il.DeclareLocal(runtimeType);

        if (constructorPlan.Constructor is not null && constructorPlan.Parameters.Length > 0)
        {
            EmitConstructorArguments(il, constructorPlan, reader);
            il.Emit(OpCodes.Newobj, constructorPlan.Constructor);
            il.Emit(OpCodes.Stloc, entity);
        }
        else
        {
            var ctor = runtimeType.GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException($"{runtimeType.FullName} must have either a parameterless constructor or a constructor whose parameter names match result columns for MSIL/record materialization.");

            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc, entity);
        }

        var constructorParameterNames = constructorPlan.ParameterNames;
        var properties = runtimeType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && Support.IsScalarColumn(p))
            .Where(p => !constructorParameterNames.Contains(p.Name))
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


    private sealed record ForgeConstructorPlan(
        ConstructorInfo? Constructor,
        ParameterInfo[] Parameters,
        int[] Ordinals,
        HashSet<string> ParameterNames);

    private static ForgeConstructorPlan CreateConstructorPlan(Type type, DbDataReader reader)
    {
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            columns[reader.GetName(i)] = i;

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(c => c.GetParameters().Length > 0)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToArray();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var ordinals = new int[parameters.Length];
            var matched = 0;
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterName = parameters[i].Name ?? string.Empty;
                names.Add(parameterName);
                if (columns.TryGetValue(parameterName, out var ordinal))
                {
                    ordinals[i] = ordinal;
                    matched++;
                }
                else
                {
                    ordinals[i] = -1;
                }
            }

            // Record/constructor DTO support: prefer constructors where all parameters are present.
            // If some projection columns are missing, ForgeORM still constructs the record using default values.
            // This supports DTOs like ProductListItem where BrandName/CategoryName may be absent in a specific query.
            // A constructor must match at least one result column to avoid choosing an unrelated constructor.
            if (matched > 0 || parameters.All(p => p.HasDefaultValue))
                return new ForgeConstructorPlan(constructor, parameters, ordinals, names);
        }

        return new ForgeConstructorPlan(null, Array.Empty<ParameterInfo>(), Array.Empty<int>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static void EmitConstructorArguments(ILGenerator il, ForgeConstructorPlan plan, DbDataReader reader)
    {
        var isDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull), new[] { typeof(int) })!;

        for (var i = 0; i < plan.Parameters.Length; i++)
        {
            var parameter = plan.Parameters[i];
            var parameterType = parameter.ParameterType;
            var ordinal = plan.Ordinals[i];
            var endLabel = il.DefineLabel();
            var nullLabel = il.DefineLabel();
            var local = il.DeclareLocal(parameterType);

            if (ordinal < 0)
            {
                EmitDefaultValue(il, parameterType);
                continue;
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ordinal);
            il.Emit(OpCodes.Callvirt, isDbNull);
            il.Emit(OpCodes.Brtrue_S, nullLabel);

            EmitReadValueForProperty(il, parameterType, ordinal);
            il.Emit(OpCodes.Stloc, local);
            il.Emit(OpCodes.Br_S, endLabel);

            il.MarkLabel(nullLabel);
            il.Emit(OpCodes.Ldloca_S, local);
            il.Emit(OpCodes.Initobj, parameterType);

            il.MarkLabel(endLabel);
            il.Emit(OpCodes.Ldloc, local);
        }
    }

    private static void EmitDefaultValue(ILGenerator il, Type type)
    {
        if (type.IsValueType)
        {
            var local = il.DeclareLocal(type);
            il.Emit(OpCodes.Ldloca_S, local);
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Ldloc, local);
        }
        else
        {
            il.Emit(OpCodes.Ldnull);
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

        object? value = ForgeRuntimeAccessorCache.DefaultValue(type);

        ForgeRuntimeAccessorCache.Set(identityProperty, entity, value);
    }
}
