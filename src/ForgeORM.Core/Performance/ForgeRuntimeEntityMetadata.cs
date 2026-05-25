using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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
        var table = type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? type.Name;
        var sourceProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var buffer = new ForgeRuntimePropertyPlan[sourceProperties.Length];
        var count = 0;

        for (var i = 0; i < sourceProperties.Length; i++)
        {
            var property = sourceProperties[i];
            if (!property.CanRead || !ForgeMaterializer.IsScalar(property.PropertyType))
                continue;

            buffer[count++] = CreatePropertyPlan(property);
        }

        var properties = new ForgeRuntimePropertyPlan[count];
        if (count != 0)
            Array.Copy(buffer, properties, count);

        ForgeRuntimePropertyPlan? key = null;
        for (var i = 0; i < properties.Length; i++)
        {
            if (properties[i].IsKey)
            {
                key = properties[i];
                break;
            }
        }

        if (key is null)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                if (!properties[i].PropertyName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    continue;

                key = properties[i];
                break;
            }
        }

        var selectColumns = BuildSelectColumns(properties);
        var insertColumns = new StringBuilder(properties.Length * 16);
        var insertValues = new StringBuilder(properties.Length * 16);
        var updateSet = new StringBuilder(properties.Length * 24);
        var insertableCount = 0;
        var updateableCount = 0;

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            if (property.IsKey || property.IsComputed)
                continue;

            if (insertColumns.Length != 0)
            {
                insertColumns.Append(", ");
                insertValues.Append(", ");
            }

            insertColumns.Append(property.ColumnName);
            insertValues.Append('@').Append(property.PropertyName);
            insertableCount++;

            if (updateSet.Length != 0)
                updateSet.Append(", ");

            updateSet.Append(property.ColumnName).Append(" = @").Append(property.PropertyName);
            updateableCount++;
        }

        return new ForgeRuntimeEntityPlan
        {
            EntityType = type,
            TableName = table,
            Properties = properties,
            Key = key,
            SelectColumnsSql = selectColumns,
            InsertSql = insertableCount == 0 ? $"-- No insertable columns for {type.Name}" : $"INSERT INTO {table} ({insertColumns}) VALUES ({insertValues});",
            UpdateSql = key is null ? $"-- No key for {type.Name}" : $"UPDATE {table} SET {updateSet} WHERE {key.ColumnName} = @{key.PropertyName};",
            DeleteSql = key is null ? $"-- No key for {type.Name}" : $"DELETE FROM {table} WHERE {key.ColumnName} = @{key.PropertyName};"
        };
    }

    private static string BuildSelectColumns(ForgeRuntimePropertyPlan[] properties)
    {
        if (properties.Length == 0)
            return "*";

        var builder = new StringBuilder(properties.Length * 16);
        for (var i = 0; i < properties.Length; i++)
        {
            if (builder.Length != 0)
                builder.Append(", ");

            builder.Append(properties[i].ColumnName);
        }

        return builder.ToString();
    }

    private static ForgeRuntimePropertyPlan CreatePropertyPlan(PropertyInfo property)
    {
        var column = property.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? property.Name;
        var isKey = property.GetCustomAttribute<ForgeKeyAttribute>() is not null || property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
        var isComputed = property.GetCustomAttribute<ForgeComputedAttribute>() is not null;

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
