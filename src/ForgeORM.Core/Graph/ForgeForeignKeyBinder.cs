using ForgeORM.Core;
using System.Reflection;

namespace ForgeORM.Core.Graph;

/// <summary>
/// Convention-based foreign-key binder.
/// </summary>
public sealed class ForgeForeignKeyBinder : IForgeForeignKeyBinder
{
    private readonly IForgeKeyResolver _keyResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgeForeignKeyBinder"/> class.
    /// </summary>
    public ForgeForeignKeyBinder(IForgeKeyResolver keyResolver)
    {
        _keyResolver = keyResolver;
    }

    /// <inheritdoc />
    public void Bind(object parent, object child, ForgeGraphIdentityMap identityMap)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(identityMap);

        var parentMetadata = ForgeEntityMetadataCache.Get(parent.GetType());
        var parentKey = identityMap.GetDatabaseKeyOrDefault(parent) ?? _keyResolver.GetKey(parent);

        if (parentMetadata.KeyProperty is null || parentKey is null)
        {
            return;
        }

        var fk = FindForeignKey(parent.GetType(), child.GetType(), parentMetadata.KeyProperty.Name);
        if (fk is null || !fk.CanWrite)
        {
            return;
        }

        ForgeRuntimeAccessorCache.Set(fk, child, ConvertValue(parentKey, fk.PropertyType));
    }

    /// <summary>
    /// Attempts to discover the child foreign-key property for a parent-child pair.
    /// </summary>
    public static PropertyInfo? FindForeignKey(Type parentType, Type childType, string parentKeyName = "Id")
    {
        var candidates = childType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.GetIndexParameters().Length == 0)
            .ToArray();

        var expectedNames = new[]
        {
            parentType.Name + parentKeyName,
            parentType.Name + "Id",
            parentType.Name.TrimEnd('y') + "Id"
        };

        return candidates.FirstOrDefault(x => expectedNames.Any(n => string.Equals(x.Name, n, StringComparison.OrdinalIgnoreCase)))
            ?? candidates.FirstOrDefault(x => x.GetCustomAttributes(false).Any(a => a.GetType().Name is "ForgeForeignKeyAttribute" or "ForeignKeyAttribute"));
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsInstanceOfType(value))
        {
            return value;
        }

        if (underlying == typeof(Guid))
        {
            return value is Guid guid ? guid : Guid.Parse(value.ToString()!);
        }

        return Convert.ChangeType(value, underlying);
    }
}
