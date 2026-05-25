namespace ForgeORM.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ForgeTableAttribute : Attribute
{
    /// <summary>
    /// Executes the ForgeTableAttribute operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ForgeTableAttribute operation.</returns>
    public string Name { get; }
    /// <summary>
    /// Executes the ForgeTableAttribute operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ForgeTableAttribute operation.</returns>
    public ForgeTableAttribute(string name) => Name = name;
}
