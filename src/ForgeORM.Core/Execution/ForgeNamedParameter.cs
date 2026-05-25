using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Parameter aliases used by normalized SQL-builder, split-query and relationship paths.
/// </summary>
public readonly struct ForgeNamedParameter<TValue>
{
    public ForgeNamedParameter(string name, TValue value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public TValue Value { get; }
}
