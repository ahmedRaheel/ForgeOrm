namespace ForgeORM.Core.Graph;

/// <summary>
/// Reflection-based default key resolver with cached metadata.
/// </summary>
public sealed class ForgeKeyResolver : IForgeKeyResolver
{
    /// <inheritdoc />
    public object? GetKey(object entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var metadata = ForgeEntityMetadataCache.Get(entity.GetType());
        return metadata.KeyProperty?.GetValue(entity);
    }

    /// <inheritdoc />
    public void SetKey(object entity, object? key)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var metadata = ForgeEntityMetadataCache.Get(entity.GetType());

        if (metadata.KeyProperty is null || !metadata.KeyProperty.CanWrite)
        {
            return;
        }

        metadata.KeyProperty.SetValue(entity, ConvertValue(key, metadata.KeyProperty.PropertyType));
    }

    /// <inheritdoc />
    public bool HasKey(object entity)
    {
        var key = GetKey(entity);
        return !IsDefaultValue(key);
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

        if (underlying.IsEnum)
        {
            return Enum.Parse(underlying, value.ToString()!, ignoreCase: true);
        }

        return Convert.ChangeType(value, underlying);
    }

    private static bool IsDefaultValue(object? value)
    {
        if (value is null)
        {
            return true;
        }

        var type = value.GetType();
        var defaultValue = type.IsValueType ? Activator.CreateInstance(type) : null;
        return Equals(value, defaultValue);
    }
}
