namespace ForgeORM.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ForgeTableAttribute : Attribute
{
    public string Name { get; }
    public ForgeTableAttribute(string name) => Name = name;
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeKeyAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeCodeAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public sealed class ForgeColumnAttribute : Attribute
{
    public string Name { get; }
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
    public ForgeEnumStorage Storage { get; }
    public ForgeEnumStorageAttribute(ForgeEnumStorage storage) => Storage = storage;
}
