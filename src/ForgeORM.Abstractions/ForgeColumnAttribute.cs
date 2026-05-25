namespace ForgeORM.Abstractions;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeColumnAttribute : Attribute
{
    /// <summary>
    /// Executes the ForgeColumnAttribute operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ForgeColumnAttribute operation.</returns>
    public string Name { get; }
    /// <summary>
    /// Executes the ForgeColumnAttribute operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the ForgeColumnAttribute operation.</returns>
    public ForgeColumnAttribute(string name) => Name = name;
}
