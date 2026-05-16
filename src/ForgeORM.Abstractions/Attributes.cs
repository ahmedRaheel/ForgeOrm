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

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeKeyAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeCodeAttribute : Attribute { }

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

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeComputedAttribute : Attribute { }


public enum ForgeEnumStorage
{
    String = 0,
    Number = 1
}

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
