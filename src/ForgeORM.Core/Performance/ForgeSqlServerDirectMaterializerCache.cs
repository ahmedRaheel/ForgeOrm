using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Data.SqlClient;
using ForgeORM.Abstractions;
using ForgeORM.Core;
using static ForgeORM.Core.ForgeIlMaterializerCache;

namespace ForgeORM.Core.Performance;

/// <summary>
/// SQL Server concrete reader materializer. This emits delegates that accept SqlDataReader directly,
/// allowing provider-direct hot paths to avoid DbDataReader virtual dispatch in the row loop.
/// Reflection is used only once when a materializer is built and never during per-row execution.
/// </summary>
internal static class ForgeSqlServerDirectMaterializerCache
{
    private static readonly ConcurrentDictionary<string, Delegate> Cache = new(StringComparer.Ordinal);

    public static Func<SqlDataReader, T> GetOrCreate<T>(SqlDataReader reader)
    {
        if (ForgeSourceGeneratedRegistry.CompilationMode != ForgeOrmCompilationMode.RuntimeEmit)
        {
            if (ForgeSourceGeneratedRegistry.TryGetProvider(typeof(T), out var provider)
                && provider.TryCreateReader<T>(reader, out var generated)
                && generated is not null)
            {
                return r => generated(r);
            }

            if (ForgeSourceGeneratedRegistry.CompilationMode == ForgeOrmCompilationMode.SourceGeneratedStrict)
                throw new InvalidOperationException($"No ForgeORM source-generated SQL Server direct materializer was registered for {typeof(T).FullName}.");
        }

        var type = typeof(T);
        var key = CreateKey(type, reader);
        return (Func<SqlDataReader, T>)Cache.GetOrAdd(key, _ => Build<T>(reader));
    }

    private static string CreateKey(Type type, SqlDataReader reader)
    {
        var parts = new string[(reader.FieldCount * 2) + 2];
        parts[0] = type.FullName ?? type.Name;
        parts[1] = ForgeSourceGeneratedRegistry.CompilationMode.ToString();
        var index = 2;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            parts[index++] = Normalize(reader.GetName(i));
            parts[index++] = reader.GetFieldType(i).FullName ?? reader.GetFieldType(i).Name;
        }
        return string.Join('|', parts);
    }

    private static Func<SqlDataReader, T> Build<T>(SqlDataReader reader)
    {
        var targetType = typeof(T);
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        var method = new DynamicMethod(
            $"ForgeORM_SqlServerDirect_{Sanitize(actualType.FullName ?? actualType.Name)}_{Guid.NewGuid():N}",
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

        var constructorParameterNames = ctorPlan.ParameterNames;
        var properties = actualType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && Support.IsScalarColumn(p))
            .Where(p => !constructorParameterNames.Contains(Normalize(p.Name)))
            .Select(p => new { Property = p, Column = Normalize(p.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? p.Name) })
            .GroupBy(x => x.Column, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Property, StringComparer.OrdinalIgnoreCase);

        var isDbNull = typeof(SqlDataReader).GetMethod(nameof(SqlDataReader.IsDBNull), new[] { typeof(int) })!;

        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            var column = Normalize(reader.GetName(ordinal));
            if (!properties.TryGetValue(column, out var property))
                continue;

            var setter = property.SetMethod;
            if (setter is null)
                continue;

            var end = il.DefineLabel();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, ordinal);
            il.Emit(OpCodes.Callvirt, isDbNull);
            il.Emit(OpCodes.Brtrue_S, end);

            il.Emit(OpCodes.Ldloc, entity);
            EmitReadValue(il, property.PropertyType, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType, ordinal);
            il.Emit(OpCodes.Callvirt, setter);
            il.MarkLabel(end);
        }

        il.Emit(OpCodes.Ldloc, entity);
        il.Emit(OpCodes.Ret);
        return (Func<SqlDataReader, T>)method.CreateDelegate(typeof(Func<SqlDataReader, T>));
    }

    private static void EmitScalarReturn(ILGenerator il, Type targetType, Type actualType)
    {
        var isDbNull = typeof(SqlDataReader).GetMethod(nameof(SqlDataReader.IsDBNull), new[] { typeof(int) })!;
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

        var getter = typeof(SqlDataReader).GetMethod(nameof(SqlDataReader.GetFieldValue), new[] { typeof(int) })!
            .MakeGenericMethod(valueType);
        il.Emit(OpCodes.Callvirt, getter);

        if (nullableUnderlying is not null)
        {
            var ctor = finalType.GetConstructor(new[] { nullableUnderlying })!;
            il.Emit(OpCodes.Newobj, ctor);
        }
    }

    private static ConstructorPlan CreateConstructorPlan(Type type, SqlDataReader reader)
    {
        var columns = Enumerable.Range(0, reader.FieldCount)
            .ToDictionary(i => Normalize(reader.GetName(i)), i => i, StringComparer.OrdinalIgnoreCase);

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .ToArray();

        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length == 0)
                continue;

            var bindings = new ConstructorParameterBinding[parameters.Length];
            var ok = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var normalized = Normalize(parameter.Name ?? string.Empty);
                if (columns.TryGetValue(normalized, out var ordinal))
                {
                    bindings[i] = new ConstructorParameterBinding(parameter.ParameterType, ordinal, HasColumn: true);
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

            if (ok)
                return new ConstructorPlan(ctor, bindings, parameters.Select(p => Normalize(p.Name ?? string.Empty)).ToHashSet(StringComparer.OrdinalIgnoreCase));
        }

        return new ConstructorPlan(null, Array.Empty<ConstructorParameterBinding>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static void EmitConstructorArguments(ILGenerator il, ConstructorPlan plan)
    {
        foreach (var parameter in plan.Parameters)
        {
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
            var isDbNull = typeof(SqlDataReader).GetMethod(nameof(SqlDataReader.IsDBNull), new[] { typeof(int) })!;

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, parameter.Ordinal);
            il.Emit(OpCodes.Callvirt, isDbNull);
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

    private static bool IsNullable(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;
        Span<char> buffer = stackalloc char[name.Length];
        var index = 0;
        foreach (var ch in name)
        {
            if (ch is '_' or '-' or ' ')
                continue;
            buffer[index++] = char.ToUpperInvariant(ch);
        }
        return new string(buffer[..index]);
    }

    private static string Sanitize(string value)
    {
        var chars = value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
        return new string(chars);
    }

    private sealed record ConstructorPlan(ConstructorInfo? Constructor, ConstructorParameterBinding[] Parameters, HashSet<string> ParameterNames);
    private readonly record struct ConstructorParameterBinding(Type ParameterType, int Ordinal, bool HasColumn);
}
