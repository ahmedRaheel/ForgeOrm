using System.Collections.Concurrent;
using System.Reflection;

namespace ForgeORM.Core.Graph;

/// <summary>
/// Provides cached entity metadata and avoids repeated reflection during graph operations.
/// </summary>
public static class ForgeEntityMetadataCache
{
    private static readonly ConcurrentDictionary<Type, ForgeEntityMetadata> Cache = new();

    /// <summary>
    /// Gets metadata for the supplied entity type.
    /// </summary>
    public static ForgeEntityMetadata Get(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Cache.GetOrAdd(type, Create);
    }

    /// <summary>
    /// Gets metadata for the supplied entity type.
    /// </summary>
    public static ForgeEntityMetadata Get<T>() where T : class => Get(typeof(T));

    private static ForgeEntityMetadata Create(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.GetIndexParameters().Length == 0)
            .ToArray();

        var children = properties.Where(IsChildCollection).ToArray();
        var scalars = properties.Except(children).Where(IsScalarLike).ToArray();
        var key = FindKeyProperty(properties);

        return new ForgeEntityMetadata
        {
            EntityType = type,
            TableName = ResolveTableName(type),
            KeyProperty = key,
            IsIdentityKey = key is not null && IsIdentity(key),
            ScalarProperties = scalars,
            ChildCollections = children
        };
    }

    private static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttributes(false)
            .FirstOrDefault(x => x.GetType().Name is "ForgeTableAttribute" or "TableAttribute");

        if (attr is null)
        {
            return type.Name;
        }

        var nameProperty = attr.GetType().GetProperty("Name")
            ?? attr.GetType().GetProperty("TableName");

        return nameProperty?.GetValue(attr)?.ToString() ?? type.Name;
    }

    private static PropertyInfo? FindKeyProperty(IEnumerable<PropertyInfo> properties)
    {
        var array = properties.ToArray();

        var declaringName = array.FirstOrDefault()?.DeclaringType?.Name ?? string.Empty;

        return array.FirstOrDefault(HasKeyAttribute)
            ?? array.FirstOrDefault(x => string.Equals(x.Name, "Id", StringComparison.OrdinalIgnoreCase))
            ?? array.FirstOrDefault(x => string.Equals(x.Name, declaringName + "Id", StringComparison.OrdinalIgnoreCase))
            ?? array.FirstOrDefault(x => x.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasKeyAttribute(PropertyInfo property)
    {
        return property.GetCustomAttributes(false)
            .Any(x => x.GetType().Name is "ForgeKeyAttribute" or "KeyAttribute");
    }

    private static bool IsIdentity(PropertyInfo property)
    {
        return property.GetCustomAttributes(false)
            .Any(x => x.GetType().Name is "ForgeIdentityAttribute" or "DatabaseGeneratedAttribute")
            || property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsChildCollection(PropertyInfo property)
    {
        if (property.PropertyType == typeof(string))
        {
            return false;
        }

        if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
        {
            return false;
        }

        return property.PropertyType.IsGenericType;
    }

    private static bool IsScalarLike(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(byte[]);
    }
}
