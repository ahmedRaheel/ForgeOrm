using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using ForgeORM.Abstractions;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Cached scalar property metadata for one entity type. Reflection is used only when this plan is built.
/// </summary>
public sealed class ForgeRuntimeEntityPlan
{
    public required Type EntityType { get; init; }
    public required string TableName { get; init; }
    public required IReadOnlyList<ForgeRuntimePropertyPlan> Properties { get; init; }
    public required ForgeRuntimePropertyPlan? Key { get; init; }
    public required string SelectColumnsSql { get; init; }
    public required string InsertSql { get; init; }
    public required string UpdateSql { get; init; }
    public required string DeleteSql { get; init; }
}

/// <summary>
/// Cached property accessor with MSIL getter/setter delegates.
/// </summary>
public sealed class ForgeRuntimePropertyPlan
{
    public required string PropertyName { get; init; }
    public required string ColumnName { get; init; }
    public required Type PropertyType { get; init; }
    public required bool IsKey { get; init; }
    public required bool IsComputed { get; init; }
    public required Func<object, object?> Getter { get; init; }
    public required Action<object, object?>? Setter { get; init; }
}

/// <summary>
/// Entity metadata cache used by the high-performance execution pipeline.
/// </summary>
public static class ForgeRuntimeEntityMetadataCache
{
    private static readonly ConcurrentDictionary<Type, ForgeRuntimeEntityPlan> Cache = new();

    public static ForgeRuntimeEntityPlan For<TEntity>() => For(typeof(TEntity));

    public static ForgeRuntimeEntityPlan For(Type entityType) => Cache.GetOrAdd(entityType, Build);

    public static void PreWarm(params Type[] entityTypes)
    {
        foreach (var entityType in entityTypes)
            _ = For(entityType);
    }

    private static ForgeRuntimeEntityPlan Build(Type type)
    {
        var table = ResolveTableName(type);

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead && ForgeMaterializer.IsScalar(x.PropertyType))
            .Select(CreatePropertyPlan)
            .ToArray();

        var key = properties.FirstOrDefault(x => x.IsKey)
                  ?? properties.FirstOrDefault(x => x.PropertyName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                  ?? properties.FirstOrDefault(x => x.PropertyName.Equals(type.Name + "Id", StringComparison.OrdinalIgnoreCase))
                  ?? properties.FirstOrDefault(x => x.PropertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

        var insertable = properties.Where(x => !x.IsKey && !x.IsComputed).ToArray();
        var updateable = properties.Where(x => !x.IsKey && !x.IsComputed).ToArray();
        var selectColumns = properties.Length == 0 ? "*" : string.Join(", ", properties.Select(x => x.ColumnName));
        var insertColumns = string.Join(", ", insertable.Select(x => x.ColumnName));
        var insertValues = string.Join(", ", insertable.Select(x => "@" + x.PropertyName));
        var updateSet = string.Join(", ", updateable.Select(x => $"{x.ColumnName} = @{x.PropertyName}"));

        return new ForgeRuntimeEntityPlan
        {
            EntityType = type,
            TableName = table,
            Properties = properties,
            Key = key,
            SelectColumnsSql = selectColumns,
            InsertSql = insertable.Length == 0 ? $"-- No insertable columns for {type.Name}" : $"INSERT INTO {table} ({insertColumns}) VALUES ({insertValues});",
            UpdateSql = key is null ? $"-- No key for {type.Name}" : $"UPDATE {table} SET {updateSet} WHERE {key.ColumnName} = @{key.PropertyName};",
            DeleteSql = key is null ? $"-- No key for {type.Name}" : $"DELETE FROM {table} WHERE {key.ColumnName} = @{key.PropertyName};"
        };
    }

    private static ForgeRuntimePropertyPlan CreatePropertyPlan(PropertyInfo property)
    {
        var column = ResolveColumnName(property);
        var declaringName = property.DeclaringType?.Name ?? string.Empty;
        var isKey = HasAttributeNamed(property, "ForgeKeyAttribute")
                    || HasAttributeNamed(property, "KeyAttribute")
                    || property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)
                    || property.Name.Equals(declaringName + "Id", StringComparison.OrdinalIgnoreCase);
        var isComputed = HasAttributeNamed(property, "ForgeComputedAttribute") || HasAttributeNamed(property, "ComputedAttribute");

        return new ForgeRuntimePropertyPlan
        {
            PropertyName = property.Name,
            ColumnName = column,
            PropertyType = property.PropertyType,
            IsKey = isKey,
            IsComputed = isComputed,
            Getter = BuildGetter(property),
            Setter = property.CanWrite ? BuildSetter(property) : null
        };
    }


    private static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttributes(false)
            .FirstOrDefault(x => x.GetType().Name is "ForgeTableAttribute" or "TableAttribute");

        if (attr is null)
            return type.Name;

        var name = attr.GetType().GetProperty("Name")?.GetValue(attr)?.ToString()
                   ?? attr.GetType().GetProperty("TableName")?.GetValue(attr)?.ToString();

        return string.IsNullOrWhiteSpace(name) ? type.Name : name!;
    }

    private static string ResolveColumnName(PropertyInfo property)
    {
        var attr = property.GetCustomAttributes(false)
            .FirstOrDefault(x => x.GetType().Name is "ForgeColumnAttribute" or "ColumnAttribute");

        if (attr is null)
            return property.Name;

        var name = attr.GetType().GetProperty("Name")?.GetValue(attr)?.ToString()
                   ?? attr.GetType().GetProperty("ColumnName")?.GetValue(attr)?.ToString();

        return string.IsNullOrWhiteSpace(name) ? property.Name : name!;
    }

    private static bool HasAttributeNamed(MemberInfo member, string attributeName)
        => member.GetCustomAttributes(false).Any(x => x.GetType().Name.Equals(attributeName, StringComparison.OrdinalIgnoreCase));

    private static Func<object, object?> BuildGetter(PropertyInfo property)
    {
        var declaring = property.DeclaringType!;
        var method = new DynamicMethod($"ForgeORM_Get_{declaring.Name}_{property.Name}_{Guid.NewGuid():N}", typeof(object), new[] { typeof(object) }, typeof(ForgeRuntimeEntityMetadataCache), true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(declaring.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, declaring);
        il.Emit(declaring.IsValueType ? OpCodes.Call : OpCodes.Callvirt, property.GetMethod!);
        if (property.PropertyType.IsValueType)
            il.Emit(OpCodes.Box, property.PropertyType);
        il.Emit(OpCodes.Ret);
        return (Func<object, object?>)method.CreateDelegate(typeof(Func<object, object?>));
    }

    private static Action<object, object?> BuildSetter(PropertyInfo property)
    {
        var declaring = property.DeclaringType!;
        var method = new DynamicMethod($"ForgeORM_Set_{declaring.Name}_{property.Name}_{Guid.NewGuid():N}", typeof(void), new[] { typeof(object), typeof(object) }, typeof(ForgeRuntimeEntityMetadataCache), true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(declaring.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaring);
        il.Emit(OpCodes.Ldarg_1);
        if (property.PropertyType.IsValueType)
            il.Emit(OpCodes.Unbox_Any, property.PropertyType);
        else
            il.Emit(OpCodes.Castclass, property.PropertyType);
        il.Emit(declaring.IsValueType ? OpCodes.Call : OpCodes.Callvirt, property.SetMethod!);
        il.Emit(OpCodes.Ret);
        return (Action<object, object?>)method.CreateDelegate(typeof(Action<object, object?>));
    }
}
