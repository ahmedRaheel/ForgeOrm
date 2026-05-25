namespace ForgeORM.Abstractions;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ForgeEnumStorageAttribute : Attribute
{
    /// <summary>
    /// Executes the ForgeEnumStorageAttribute operation.
    /// </summary>
    /// <param name="storage">The storage value.</param>
    /// <returns>The result of the ForgeEnumStorageAttribute operation.</returns>
    public ForgeEnumStorage Storage { get; }
    /// <summary>
    /// Executes the ForgeEnumStorageAttribute operation.
    /// </summary>
    /// <param name="storage">The storage value.</param>
    /// <returns>The result of the ForgeEnumStorageAttribute operation.</returns>
    public ForgeEnumStorageAttribute(ForgeEnumStorage storage) => Storage = storage;
}
