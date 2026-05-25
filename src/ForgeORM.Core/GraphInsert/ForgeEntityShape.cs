using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core;

internal sealed class ForgeEntityShape
{
    /// <summary>
    /// Executes the For operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the For operation.</returns>
    public required string TableName { get; init; }
    /// <summary>
    /// Executes the For operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the For operation.</returns>
    public required PropertyInfo? KeyProperty { get; init; }
    /// <summary>
    /// Executes the For operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the For operation.</returns>
    public required IReadOnlyList<PropertyInfo> ScalarProperties { get; init; }

    /// <summary>
    /// Executes the For operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the For operation.</returns>
    public static ForgeEntityShape For(Type type)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && IsScalarColumnType(p.PropertyType))
            .ToList();

        return new ForgeEntityShape
        {
            TableName = ResolveTableName(type),
            KeyProperty = props.FirstOrDefault(p => p.GetCustomAttribute<ForgeKeyAttribute>() is not null)
                ?? props.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)),
            ScalarProperties = props
        };
    }

    /// <summary>
    /// Executes the ResolveTableName operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the ResolveTableName operation.</returns>
    public static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttribute<ForgeTableAttribute>();
        if (attr is not null) return attr.Name;
        return type.Name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? type.Name : type.Name + "s";
    }

    /// <summary>
    /// Executes the ColumnName operation.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <returns>The result of the ColumnName operation.</returns>
    public static string ColumnName(PropertyInfo property)
        => property.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? property.Name;

    /// <summary>
    /// Executes the IsComputed operation.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <returns>The result of the IsComputed operation.</returns>
    public static bool IsComputed(PropertyInfo property)
        => property.GetCustomAttribute<ForgeComputedAttribute>() is not null;

    /// <summary>
    /// Executes the EnsureGeneratedKey operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    public static void EnsureGeneratedKey(object entity)
    {
        var shape = For(entity.GetType());
        var key = shape.KeyProperty;
        if (key is null || !key.CanWrite) return;
        var type = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;
        var current = ForgeRuntimeAccessorCache.Get(key, entity);
        if (type == typeof(Guid) && (current is null || (Guid)current == Guid.Empty))
            ForgeRuntimeAccessorCache.Set(key, entity, Guid.NewGuid());
    }

    private static bool IsScalarColumnType(Type type)
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

    private static bool IsEnumerableButNotString(Type type)
        => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}
