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
        => ForgeReaderShapeCache.CreateKey(type, reader);

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

        var properties = BuildWritableScalarProperties(actualType, ctorPlan.ParameterNames);

        var isDbNull = typeof(SqlDataReader).GetMethod(nameof(SqlDataReader.IsDBNull), new[] { typeof(int) })!;

        for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
        {
            var column = reader.GetName(ordinal);
            if (!TryFindMappedProperty(properties, column, out var property))
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
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 0)
            return EmptyConstructorPlan();

        Array.Sort(constructors, static (left, right) =>
            right.GetParameters().Length.CompareTo(left.GetParameters().Length));

        for (var c = 0; c < constructors.Length; c++)
        {
            var ctor = constructors[c];
            var parameters = ctor.GetParameters();
            if (parameters.Length == 0)
                continue;

            var bindings = new ConstructorParameterBinding[parameters.Length];
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ok = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var normalized = Normalize(parameter.Name ?? string.Empty);
                names.Add(normalized);
                var ordinal = FindOrdinal(reader, normalized);
                if (ordinal >= 0)
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
                return new ConstructorPlan(ctor, bindings, names);
        }

        return EmptyConstructorPlan();
    }

    private static ConstructorPlan EmptyConstructorPlan()
        => new(null, Array.Empty<ConstructorParameterBinding>(), new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static int FindOrdinal(SqlDataReader reader, string normalizedColumn)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(Normalize(reader.GetName(i)), normalizedColumn, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
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


    private readonly struct MappedProperty
    {
        public readonly string ColumnName;
        public readonly PropertyInfo Property;

        public MappedProperty(string columnName, PropertyInfo property)
        {
            ColumnName = columnName;
            Property = property;
        }
    }

    private static MappedProperty[] BuildWritableScalarProperties(Type type, HashSet<string> constructorParameterNames)
    {
        var source = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (source.Length == 0)
            return Array.Empty<MappedProperty>();

        var buffer = new MappedProperty[source.Length];
        var count = 0;
        for (var i = 0; i < source.Length; i++)
        {
            var property = source[i];
            if (!property.CanWrite || !Support.IsScalarColumn(property) || constructorParameterNames.Contains(Normalize(property.Name)))
                continue;

            var columnAttribute = property.GetCustomAttribute<ForgeColumnAttribute>();
            buffer[count++] = new MappedProperty(columnAttribute?.Name ?? property.Name, property);
        }

        if (count == 0)
            return Array.Empty<MappedProperty>();

        if (count == buffer.Length)
            return buffer;

        var result = new MappedProperty[count];
        Array.Copy(buffer, result, count);
        return result;
    }

    private static bool TryFindMappedProperty(MappedProperty[] properties, string columnName, out PropertyInfo property)
    {
        var normalized = Normalize(columnName);
        for (var i = 0; i < properties.Length; i++)
        {
            if (string.Equals(Normalize(properties[i].ColumnName), normalized, StringComparison.OrdinalIgnoreCase))
            {
                property = properties[i].Property;
                return true;
            }
        }

        property = null!;
        return false;
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
        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
                chars[i] = '_';
        }

        return new string(chars);
    }

    private sealed record ConstructorPlan(ConstructorInfo? Constructor, ConstructorParameterBinding[] Parameters, HashSet<string> ParameterNames);
    private readonly record struct ConstructorParameterBinding(Type ParameterType, int Ordinal, bool HasColumn);
}
