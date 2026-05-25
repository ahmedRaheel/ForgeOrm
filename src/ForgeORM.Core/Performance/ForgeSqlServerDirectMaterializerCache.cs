using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Data.SqlClient;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Core.Performance;

/// <summary>
/// SQL Server concrete reader materializer. This emits delegates that accept SqlDataReader directly,
/// avoids DbDataReader virtual dispatch, avoids string-key dictionaries and avoids LINQ in the plan builder.
/// </summary>
internal static class ForgeSqlServerDirectMaterializerCache
{
    private static readonly ConcurrentDictionary<SqlServerMaterializerKey, Delegate> Cache = new();
    private static readonly MethodInfo SqlReaderIsDbNull = typeof(SqlDataReader).GetMethod(nameof(SqlDataReader.IsDBNull), new[] { typeof(int) })!;
    private static readonly MethodInfo SqlReaderGetFieldValue = typeof(SqlDataReader).GetMethod(nameof(SqlDataReader.GetFieldValue), new[] { typeof(int) })!;

    public static Func<SqlDataReader, T> GetOrCreate<T>(SqlDataReader reader)
    {
        var mode = ForgeSourceGeneratedRegistry.CompilationMode;
        if (mode != ForgeOrmCompilationMode.RuntimeEmit && ForgeSourceGeneratedRegistry.TryGetProvider(typeof(T), out var provider))
        {
            if (provider.TryCreateReader<T>(reader, out var generated) && generated is not null)
                return r => generated(r);

            if (mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
                throw new InvalidOperationException($"SourceGeneratedStrict failed. Provider was registered for {typeof(T).FullName}, but no SQL Server reader was returned.");
        }

        var key = SqlServerMaterializerKey.Create(typeof(T), reader, mode);
        return (Func<SqlDataReader, T>)Cache.GetOrAdd(key, _ => Build<T>(reader));
    }

    private static Func<SqlDataReader, T> Build<T>(SqlDataReader reader)
    {
        var targetType = typeof(T);
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        var method = new DynamicMethod(
            $"ForgeORM_SqlServerDirect_{Sanitize(actualType.FullName ?? actualType.Name)}",
            targetType,
            new[] { typeof(SqlDataReader) },
            typeof(ForgeSqlServerDirectMaterializerCache).Module,
            skipVisibility: true);

        var il = method.GetILGenerator();

        if (ForgeMaterializer.IsScalar(actualType))
        {
            EmitScalarReturn(il, targetType, actualType);
            return (Func<SqlDataReader, T>)method.CreateDelegate(typeof(Func<SqlDataReader, T>));
        }

        var ctorPlan = CreateConstructorPlan(actualType, reader);
        var entity = il.DeclareLocal(actualType);

        if (ctorPlan.Constructor is not null && ctorPlan.Parameters.Length > 0)
        {
            EmitConstructorArguments(il, ctorPlan);
            il.Emit(OpCodes.Newobj, ctorPlan.Constructor);
            il.Emit(OpCodes.Stloc, entity);
        }
        else
        {
            var ctor = actualType.GetConstructor(Type.EmptyTypes)
                ?? throw new InvalidOperationException($"{actualType.FullName} must have either a parameterless constructor or a constructor whose parameter names match result columns for SQL Server direct materialization.");
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc, entity);
        }

        var bindings = CreatePropertyBindings(actualType, reader, ctorPlan.ParameterNames);
        for (var i = 0; i < bindings.Length; i++)
        {
            var binding = bindings[i];
            var property = binding.Property;
            var setter = property.SetMethod;
            if (setter is null) continue;

            var end = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, binding.Ordinal);
            il.Emit(OpCodes.Callvirt, SqlReaderIsDbNull);
            il.Emit(OpCodes.Brtrue_S, end);

            il.Emit(OpCodes.Ldloc, entity);
            EmitReadValue(il, property.PropertyType, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType, binding.Ordinal);
            il.Emit(OpCodes.Callvirt, setter);
            il.MarkLabel(end);
        }

        il.Emit(OpCodes.Ldloc, entity);
        il.Emit(OpCodes.Ret);
        return (Func<SqlDataReader, T>)method.CreateDelegate(typeof(Func<SqlDataReader, T>));
    }

    private static PropertyBinding[] CreatePropertyBindings(Type type, SqlDataReader reader, string[] constructorParameterNames)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var temp = new PropertyBinding[props.Length];
        var count = 0;
        for (var i = 0; i < props.Length; i++)
        {
            var property = props[i];
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

    private static void EmitScalarReturn(ILGenerator il, Type targetType, Type actualType)
    {
        var notNull = il.DefineLabel();
        var end = il.DefineLabel();
        var result = il.DeclareLocal(targetType);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Callvirt, SqlReaderIsDbNull);
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

    private static void EmitReadValue(ILGenerator il, Type finalType, Type valueType, int ordinal)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(finalType);

        if (valueType.IsEnum)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ordinal);
            var method = typeof(ForgeEnumReader)
                .GetMethod(nullableUnderlying is null ? nameof(ForgeEnumReader.ReadEnum) : nameof(ForgeEnumReader.ReadNullableEnum))!
                .MakeGenericMethod(valueType);
            il.Emit(OpCodes.Call, method);
            return;
        }

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, ordinal);
        il.Emit(OpCodes.Callvirt, SqlReaderGetFieldValue.MakeGenericMethod(valueType));

        if (nullableUnderlying is not null)
        {
            var ctor = finalType.GetConstructor(new[] { nullableUnderlying })!;
            il.Emit(OpCodes.Newobj, ctor);
        }
    }

    private static ConstructorPlan CreateConstructorPlan(Type type, SqlDataReader reader)
    {
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        SortConstructorsByParameterCountDescending(constructors);

        for (var c = 0; c < constructors.Length; c++)
        {
            var ctor = constructors[c];
            var parameters = ctor.GetParameters();
            if (parameters.Length == 0) continue;

            var bindings = new ConstructorParameterBinding[parameters.Length];
            var names = new string[parameters.Length];
            var ok = true;
            var matched = 0;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterName = parameter.Name ?? string.Empty;
                names[i] = parameterName;
                var ordinal = FindOrdinal(reader, parameterName);
                if (ordinal >= 0)
                {
                    bindings[i] = new ConstructorParameterBinding(parameter.ParameterType, ordinal, HasColumn: true);
                    matched++;
                }
                else if (parameter.HasDefaultValue || IsNullable(parameter.ParameterType))
                {
                    bindings[i] = new ConstructorParameterBinding(parameter.ParameterType, -1, HasColumn: false);
                }
                else
                {
                    ok = false;
                    break;
                }
            }

            if (ok && matched > 0)
                return new ConstructorPlan(ctor, bindings, names);
        }

        return new ConstructorPlan(null, Array.Empty<ConstructorParameterBinding>(), Array.Empty<string>());
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
            if (!parameter.HasColumn)
            {
                var local = il.DeclareLocal(parameter.ParameterType);
                il.Emit(OpCodes.Ldloca_S, local);
                il.Emit(OpCodes.Initobj, parameter.ParameterType);
                il.Emit(OpCodes.Ldloc, local);
                continue;
            }

            var end = il.DefineLabel();
            var hasValue = il.DefineLabel();
            var localValue = il.DeclareLocal(parameter.ParameterType);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, parameter.Ordinal);
            il.Emit(OpCodes.Callvirt, SqlReaderIsDbNull);
            il.Emit(OpCodes.Brfalse_S, hasValue);
            il.Emit(OpCodes.Ldloca_S, localValue);
            il.Emit(OpCodes.Initobj, parameter.ParameterType);
            il.Emit(OpCodes.Br_S, end);

            il.MarkLabel(hasValue);
            EmitReadValue(il, parameter.ParameterType, Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType, parameter.Ordinal);
            il.Emit(OpCodes.Stloc, localValue);

            il.MarkLabel(end);
            il.Emit(OpCodes.Ldloc, localValue);
        }
    }

    private static int FindOrdinal(SqlDataReader reader, string name)
    {
        for (var i = 0; i < reader.FieldCount; i++)
            if (ColumnEquals(reader.GetName(i), name)) return i;
        return -1;
    }

    private static bool ContainsName(string[] names, string name)
    {
        for (var i = 0; i < names.Length; i++)
            if (ColumnEquals(names[i], name)) return true;
        return false;
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
            if (char.ToUpperInvariant(left[li]) != char.ToUpperInvariant(right[ri])) return false;
            li++;
            ri++;
        }
    }

    private static bool IsIgnored(char c) => c is '_' or '-' or ' ' or '[' or ']' or '"';
    private static bool IsNullable(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static string Sanitize(string value)
    {
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
            if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
        return new string(chars);
    }

    private readonly record struct PropertyBinding(PropertyInfo Property, int Ordinal);
    private sealed record ConstructorPlan(ConstructorInfo? Constructor, ConstructorParameterBinding[] Parameters, string[] ParameterNames);
    private readonly record struct ConstructorParameterBinding(Type ParameterType, int Ordinal, bool HasColumn);
}
