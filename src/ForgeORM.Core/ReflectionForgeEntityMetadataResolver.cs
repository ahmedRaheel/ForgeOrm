using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ReflectionForgeEntityMetadataResolver : IForgeEntityMetadataResolver
{
    private readonly ConcurrentDictionary<Type, ForgeEntityMetadata> _cache = new();
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public ForgeEntityMetadata Resolve<T>() => Resolve(typeof(T));
    /// <summary>
    /// Executes the Resolve operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the Resolve operation.</returns>
    public ForgeEntityMetadata Resolve(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // Framework-level policy: generated metadata always wins when available, even when
        // older app code still constructs ReflectionForgeEntityMetadataResolver directly.
        // Reflection becomes the safe fallback, not a separate runtime framework.
        if (ForgeSourceGeneratedRegistry.TryGetMetadata(type, out var generated))
            return generated;

        return _cache.GetOrAdd(type, BuildMetadata);
    }

    private static ForgeEntityMetadata BuildMetadata(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && ForgeMaterializer.IsScalar(p.PropertyType))
            .ToArray();

        var keyProperty = ResolveKeyProperty(type, properties);

        var props = properties
            .Select(p => new ForgePropertyMetadata
            {
                PropertyName = p.Name,
                ColumnName = p.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? p.Name,
                PropertyType = p.PropertyType,
                IsKey = ReferenceEquals(p, keyProperty),
                IsCode = p.GetCustomAttribute<ForgeCodeAttribute>() is not null || p.Name.Equals("Code", StringComparison.OrdinalIgnoreCase),
                IsComputed = p.GetCustomAttribute<ForgeComputedAttribute>() is not null
            }).ToList();

        return new ForgeEntityMetadata
        {
            EntityType = type,
            TableName = type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? type.Name,
            KeyColumn = props.FirstOrDefault(x => x.IsKey)?.ColumnName ?? "Id",
            CodeColumn = props.FirstOrDefault(x => x.IsCode)?.ColumnName ?? "Code",
            Properties = props
        };
    }

    private static PropertyInfo? ResolveKeyProperty(Type type, PropertyInfo[] properties)
    {
        // Attribute-first when users opt in. Dapper-like convention still works without attributes.
        var explicitKey = properties.FirstOrDefault(p =>
            p.GetCustomAttribute<ForgeKeyAttribute>() is not null ||
            p.GetCustomAttribute<KeyAttribute>() is not null);
        if (explicitKey is not null)
            return explicitKey;

        var id = properties.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (id is not null)
            return id;

        var entityId = properties.FirstOrDefault(p => p.Name.Equals(type.Name + "Id", StringComparison.OrdinalIgnoreCase));
        if (entityId is not null)
            return entityId;

        // Common record/DTO convention: OrderSummaryRecord(OrderId, ...).
        var suffixId = properties.FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
        return suffixId;
    }
}
