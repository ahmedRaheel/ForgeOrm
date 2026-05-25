using ForgeORM.Abstractions;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Core;

/// <summary>
/// Dapper-style MSIL materializer cache.
/// One DynamicMethod is emitted per entity type + result column shape and reused for every row.
/// SourceGenerated mode uses registered generated readers first; RuntimeEmit uses this MSIL path.
/// </summary>
internal static class ForgeIlMaterializerCache
{
    private static readonly ConcurrentDictionary<ForgeMaterializerCacheKey, Delegate> Cache = new();
    private static readonly ConcurrentDictionary<ForgeMaterializerCacheKey, Delegate> ObjectCache = new();

    private static readonly MethodInfo DbReaderIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull), new[] { typeof(int) })!;
    private static readonly MethodInfo DbReaderGetFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue), new[] { typeof(int) })!;

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
                    throw new InvalidOperationException($"SourceGeneratedStrict failed. Provider was registered for {type.FullName}, but TryCreateReader returned {created} and reader was {(sourceReader is null ? "null" : "not null")}.");
            }
            else if (mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            {
                throw new InvalidOperationException($"SourceGeneratedStrict failed. No source-generated provider was registered for {type.FullName}.");
            }
        }

        var key = ForgeMaterializerCacheKey.Create(type, reader, ForgeOrmCompilationMode.RuntimeEmit);
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
                throw new InvalidOperationException($"SourceGeneratedStrict failed. No source-generated provider was registered for {type.FullName}.");
        }

        var key = ForgeMaterializerCacheKey.Create(type, reader, ForgeOrmCompilationMode.RuntimeEmit);
        return (Func<DbDataReader, object>)ObjectCache.GetOrAdd(key, _ => CreateObjectMaterializer(type, reader));
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
            EmitConstructorArguments(il, constructorPlan);
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

        var bindings = CreatePropertyBindings(actualType, reader, constructorPlan.ParameterNames);
        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            var property = binding.Property;
            var setter = property.SetMethod;
            if (setter is null) continue;

            var endLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, binding.Ordinal);
            il.Emit(OpCodes.Callvirt, DbReaderIsDbNull);
            il.Emit(OpCodes.Brtrue_S, endLabel);

            il.Emit(OpCodes.Ldloc, entity);
            EmitReadValueForProperty(il, property.PropertyType, binding.Ordinal);
            il.Emit(OpCodes.Callvirt, setter);
            il.MarkLabel(endLabel);
        }

        il.Emit(OpCodes.Ldloc, entity);
        il.Emit(OpCodes.Ret);

        return (Func<DbDataReader, T>)method.CreateDelegate(typeof(Func<DbDataReader, T>));
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
            if (runtimeType.IsValueType) il.Emit(OpCodes.Box, runtimeType);
            il.Emit(OpCodes.Ret);
            return (Func<DbDataReader, object>)method.CreateDelegate(typeof(Func<DbDataReader, object>));
        }

        var constructorPlan = CreateConstructorPlan(runtimeType, reader);
        var entity = il.DeclareLocal(runtimeType);

        if (constructorPlan.Constructor is not null && constructorPlan.Parameters.Length > 0)
        {
            EmitConstructorArguments(il, constructorPlan);
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

        var bindings = CreatePropertyBindings(runtimeType, reader, constructorPlan.ParameterNames);
        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            var property = binding.Property;
            var setter = property.SetMethod;
            if (setter is null) continue;

            var endLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, binding.Ordinal);
            il.Emit(OpCodes.Callvirt, DbReaderIsDbNull);
            il.Emit(OpCodes.Brtrue_S, endLabel);

            il.Emit(OpCodes.Ldloc, entity);
            EmitReadValueForProperty(il, property.PropertyType, binding.Ordinal);
            il.Emit(OpCodes.Callvirt, setter);
            il.MarkLabel(endLabel);
        }

        il.Emit(OpCodes.Ldloc, entity);
        if (runtimeType.IsValueType) il.Emit(OpCodes.Box, runtimeType);
        il.Emit(OpCodes.Ret);
        return (Func<DbDataReader, object>)method.CreateDelegate(typeof(Func<DbDataReader, object>));
    }

    private static PropertyBinding[] CreatePropertyBindings(Type type, DbDataReader reader, string[] constructorParameterNames)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var temp = new PropertyBinding[props.Length];
        var count = 0;

        for (var p = 0; p < props.Length; p++)
        {
            var property = props[p];
            if (!property.CanWrite || !Support.IsScalarColumn(property)) continue;
            if (ContainsName(constructorParameterNames, property.Name)) continue;

            var columnName = property.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? property.Name;
            var ordinal = FindOrdinal(reader, columnName);
            if (ordinal < 0 && !string.Equals(columnName, property.Name, StringComparison.OrdinalIgnoreCase))
                ordinal = FindOrdinal(reader, property.Name);
            if (ordinal < 0) continue;

            temp[count++] = new PropertyBinding(property, ordinal);
        }

        if (count == temp.Length) return temp;
        var result = new PropertyBinding[count];
        Array.Copy(temp, result, count);
        return result;
    }

    private static ConstructorPlan CreateConstructorPlan(Type type, DbDataReader reader)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        SortConstructorsByParameterCountDescending(constructors);

        for (var c = 0; c < constructors.Length; c++)
        {
            var constructor = constructors[c];
            var parameters = constructor.GetParameters();
            if (parameters.Length == 0) continue;

            var ordinals = new int[parameters.Length];
            var names = new string[parameters.Length];
            var matched = 0;
            var ok = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterName = parameter.Name ?? string.Empty;
                names[i] = parameterName;
                var ordinal = FindOrdinal(reader, parameterName);
                ordinals[i] = ordinal;

                if (ordinal >= 0) { matched++; continue; }
                if (parameter.HasDefaultValue || IsNullable(parameter.ParameterType)) continue;
                ok = false;
                break;
            }

            if (ok && matched > 0)
                return new ConstructorPlan(constructor, parameters, ordinals, names);
        }

        return new ConstructorPlan(null, Array.Empty<ParameterInfo>(), Array.Empty<int>(), Array.Empty<string>());
    }

    private static void SortConstructorsByParameterCountDescending(ConstructorInfo[] constructors)
    {
        for (var i = 1; i < constructors.Length; i++)
        {
            var item = constructors[i];
            var itemCount = item.GetParameters().Length;
            var j = i - 1;
            while (j >= 0 && constructors[j].GetParameters().Length < itemCount)
            {
                constructors[j + 1] = constructors[j];
                j--;
            }
            constructors[j + 1] = item;
        }
    }

    private static void EmitConstructorArguments(ILGenerator il, ConstructorPlan plan)
    {
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
            il.Emit(OpCodes.Callvirt, DbReaderIsDbNull);
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

    private static void EmitScalarReturn<T>(ILGenerator il, Type targetType, Type actualType)
    {
        var notNull = il.DefineLabel();
        var end = il.DefineLabel();
        var result = il.DeclareLocal(targetType);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Callvirt, DbReaderIsDbNull);
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

        if (valueType.IsEnum)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ordinal);
            var enumMethod = typeof(ForgeEnumReader)
                .GetMethod(nullableUnderlying is null ? nameof(ForgeEnumReader.ReadEnum) : nameof(ForgeEnumReader.ReadNullableEnum))!
                .MakeGenericMethod(valueType);
            il.Emit(OpCodes.Call, enumMethod);
            return;
        }

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, ordinal);
        il.Emit(OpCodes.Callvirt, DbReaderGetFieldValue.MakeGenericMethod(valueType));

        if (nullableUnderlying is not null)
        {
            var ctor = finalType.GetConstructor(new[] { nullableUnderlying })!;
            il.Emit(OpCodes.Newobj, ctor);
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

    private static int FindOrdinal(DbDataReader reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
            if (ColumnEquals(reader.GetName(i), name))
                return i;
        return -1;
    }

    private static bool ColumnEquals(string left, string right)
    {
        var li = 0;
        var ri = 0;
        while (true)
        {
            while (li < left.Length && IsIgnored(left[li])) li++;
            while (ri < right.Length && IsIgnored(right[ri])) ri++;
            if (li >= left.Length || ri >= right.Length)
                return li >= left.Length && ri >= right.Length;
            if (char.ToUpperInvariant(left[li]) != char.ToUpperInvariant(right[ri]))
                return false;
            li++;
            ri++;
        }
    }

    private static bool ContainsName(string[] names, string name)
    {
        for (var i = 0; i < names.Length; i++)
            if (ColumnEquals(names[i], name)) return true;
        return false;
    }

    private static bool IsIgnored(char c) => c == '_' || c == '-' || c == ' ' || c == '[' || c == ']' || c == '"';

    private static bool IsNullable(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static string SanitizeName(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
        return new string(chars);
    }

    private readonly record struct PropertyBinding(PropertyInfo Property, int Ordinal);
    private sealed record ConstructorPlan(ConstructorInfo? Constructor, ParameterInfo[] Parameters, int[] Ordinals, string[] ParameterNames);
}
