namespace ForgeORM.Core.Graph;

/// <summary>
/// Resolves entity keys used by graph persistence.
/// </summary>
public interface IForgeKeyResolver
{
    /// <summary>
    /// Gets the current key value from an entity.
    /// </summary>
    object? GetKey(object entity);

    /// <summary>
    /// Sets a generated key value on an entity when possible.
    /// </summary>
    void SetKey(object entity, object? key);

    /// <summary>
    /// Determines whether an entity has a non-default key value.
    /// </summary>
    bool HasKey(object entity);
}
