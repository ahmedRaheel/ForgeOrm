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

        if (mode != ForgeOrmCompilationMode.RuntimeEmit)
        {
            if (ForgeSourceGeneratedRegistry.TryGetProvider(type, out var provider))
            {
                var created = provider.TryCreateReader<T>(reader, out var sourceReader);
                if (created && sourceReader is not null)
                    return sourceReader;

                if (mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
                    throw new InvalidOperationException(
                        $"SourceGeneratedStrict failed for {type.FullName}. Provider was registered, but TryCreateReader returned {created} and reader was {(sourceReader is null ? "null" : "not null")}.");
            }
            else if (mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            {
                throw new InvalidOperationException(
                    $"SourceGeneratedStrict failed. No source-generated provider was registered for {type.FullName}. Ensure the generated assembly is referenced and its ModuleInitializer ran.");
            }
        }

        var key = mode + "|" + ForgeReaderShapeCache.CreateKey(type, reader);
        return (Func<DbDataReader, T>)Cache.GetOrAdd(key, _ => CreateMaterializer<T>(reader));
    }

    public static Func<DbDataReader, object> GetOrCreate(Type type, DbDataReader reader)
    {
        var mode = ForgeSourceGeneratedRegistry.CompilationMode;
        if (mode != ForgeOrmCompilationMode.RuntimeEmit)
        {
            if (ForgeSourceGeneratedRegistry.TryGetProvider(type, out var provider))
                return provider.GetReader(type, reader);

            if (mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
                throw new InvalidOperationException(
                    $"SourceGeneratedStrict failed. No source-generated provider was registered for {type.FullName}. Ensure the generated assembly is referenced and its ModuleInitializer ran.");
        }

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
            name: $"ForgeORM_Materialize_{SanitizeName(actualType.FullName ?? actualType.Name)}",
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

        var properties = BuildWritableScalarProperties(actualType, constructorPlan.ParameterNames);
        var isDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull), new[] { typeof(int) })!;

        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            var columnName = reader.GetName(ordinal);
            if (!TryFindMappedProperty(properties, columnName, out var property))
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

        var typedGetter = TryGetTypedDbDataReaderGetter(valueType);
        if (typedGetter is not null)
        {
            il.Emit(OpCodes.Callvirt, typedGetter);
        }
        else
        {
            var getFieldValue = typeof(DbDataReader)
                .GetMethod(nameof(DbDataReader.GetFieldValue))!
                .MakeGenericMethod(valueType);

            il.Emit(OpCodes.Callvirt, getFieldValue);
        }

        if (nullableUnderlying is not null)
        {
            var ctor = finalType.GetConstructor(new[] { nullableUnderlying })!;
            il.Emit(OpCodes.Newobj, ctor);
        }
    }

    private static MethodInfo? TryGetTypedDbDataReaderGetter(Type valueType)
    {
        valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;

        if (valueType == typeof(int)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetInt32), new[] { typeof(int) });
        if (valueType == typeof(long)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetInt64), new[] { typeof(int) });
        if (valueType == typeof(short)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetInt16), new[] { typeof(int) });
        if (valueType == typeof(byte)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetByte), new[] { typeof(int) });
        if (valueType == typeof(bool)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetBoolean), new[] { typeof(int) });
        if (valueType == typeof(string)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetString), new[] { typeof(int) });
        if (valueType == typeof(decimal)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetDecimal), new[] { typeof(int) });
        if (valueType == typeof(double)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetDouble), new[] { typeof(int) });
        if (valueType == typeof(float)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFloat), new[] { typeof(int) });
        if (valueType == typeof(Guid)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetGuid), new[] { typeof(int) });
        if (valueType == typeof(DateTime)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetDateTime), new[] { typeof(int) });
        if (valueType == typeof(char)) return typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetChar), new[] { typeof(int) });

        return null;
    }


    private static Func<DbDataReader, object> CreateObjectMaterializer(Type runtimeType, DbDataReader reader)
    {
        var method = new DynamicMethod(
            name: $"ForgeORM_Materialize_Object_{SanitizeName(runtimeType.FullName ?? runtimeType.Name)}",
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

        var properties = BuildWritableScalarProperties(runtimeType, constructorPlan.ParameterNames);
        var isDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull), new[] { typeof(int) })!;

        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            var columnName = reader.GetName(ordinal);
            if (!TryFindMappedProperty(properties, columnName, out var property))
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


    private readonly struct ForgeMappedProperty
    {
        public readonly string ColumnName;
        public readonly PropertyInfo Property;

        public ForgeMappedProperty(string columnName, PropertyInfo property)
        {
            ColumnName = columnName;
            Property = property;
        }
    }

    private static ForgeMappedProperty[] BuildWritableScalarProperties(Type type, HashSet<string> constructorParameterNames)
    {
        var source = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (source.Length == 0)
            return Array.Empty<ForgeMappedProperty>();

        var buffer = new ForgeMappedProperty[source.Length];
        var count = 0;

        for (var i = 0; i < source.Length; i++)
        {
            var property = source[i];
            if (!property.CanWrite || !Support.IsScalarColumn(property) || constructorParameterNames.Contains(property.Name))
                continue;

            var columnAttribute = property.GetCustomAttribute<ForgeColumnAttribute>();
            buffer[count++] = new ForgeMappedProperty(columnAttribute?.Name ?? property.Name, property);
        }

        if (count == 0)
            return Array.Empty<ForgeMappedProperty>();

        if (count == buffer.Length)
            return buffer;

        var result = new ForgeMappedProperty[count];
        Array.Copy(buffer, result, count);
        return result;
    }

    private static bool TryFindMappedProperty(ForgeMappedProperty[] properties, string columnName, out PropertyInfo property)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            if (string.Equals(properties[i].ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
            {
                property = properties[i].Property;
                return true;
            }
        }

        property = null!;
        return false;
    }

    private sealed record ForgeConstructorPlan(
        ConstructorInfo? Constructor,
        ParameterInfo[] Parameters,
        int[] Ordinals,
        HashSet<string> ParameterNames);

    private static ForgeConstructorPlan CreateConstructorPlan(Type type, DbDataReader reader)
    {
        var columns = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            columns[reader.GetName(i)] = i;

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 0)
            return EmptyConstructorPlan();

        Array.Sort(constructors, static (left, right) =>
            right.GetParameters().Length.CompareTo(left.GetParameters().Length));

        for (var c = 0; c < constructors.Length; c++)
        {
            var constructor = constructors[c];
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0)
                continue;

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

            if (matched > 0 || AllParametersHaveDefaults(parameters))
                return new ForgeConstructorPlan(constructor, parameters, ordinals, names);
        }

        return EmptyConstructorPlan();
    }

    private static ForgeConstructorPlan EmptyConstructorPlan()
        => new(null, Array.Empty<ParameterInfo>(), Array.Empty<int>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static bool AllParametersHaveDefaults(ParameterInfo[] parameters)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!parameters[i].HasDefaultValue)
                return false;
        }

        return true;
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
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.Length == 0)
            return "*";

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var columns = new List<string>(properties.Length);

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            if (!IsScalarColumn(property))
                continue;

            var column = property.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? property.Name;
            if (seen.Add(column))
                columns.Add(column);
        }

        return columns.Count == 0 ? "*" : string.Join(", ", columns);
    }

    public static PropertyInfo[] GetScalarProperties(
        Type type,
        bool includeIdentity = false)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.Length == 0)
            return Array.Empty<PropertyInfo>();

        var buffer = new PropertyInfo[properties.Length];
        var count = 0;

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            if (!property.CanRead || !IsScalarColumn(property))
                continue;

            if (!includeIdentity && IsIdentityColumn(property))
                continue;

            buffer[count++] = property;
        }

        if (count == 0)
            return Array.Empty<PropertyInfo>();

        if (count == buffer.Length)
            return buffer;

        var result = new PropertyInfo[count];
        Array.Copy(buffer, result, count);
        return result;
    }

    public static bool IsIdentityColumn(PropertyInfo property)
    {
        if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            return true;

        var attributes = property.GetCustomAttributes();
        foreach (var attribute in attributes)
        {
            if (attribute.GetType().Name.Contains("Key", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static object? NormalizeParameterValue(object? value)
    {
        if (value is null)
            return null;

        if (value is Enum enumValue)
        {
            return enumValue.ToString();
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
