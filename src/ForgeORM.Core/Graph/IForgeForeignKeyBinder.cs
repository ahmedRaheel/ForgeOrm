namespace ForgeORM.Core.Graph;

/// <summary>
/// Binds parent keys to child foreign-key properties during graph persistence.
/// </summary>
public interface IForgeForeignKeyBinder
{
    /// <summary>
    /// Binds the parent key to the child row foreign-key property.
    /// </summary>
    void Bind(object parent, object child, ForgeGraphIdentityMap identityMap);
}
