using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Provider-neutral single-key parameter container used by every GetById/Find/First-by-key path.
/// This keeps all lookup APIs on the same framework execution policy while allowing the parameter
/// binder cache to compile one stable shape instead of traversing anonymous objects repeatedly.
/// </summary>
public readonly struct ForgeIdParameter<TKey>
{
    public ForgeIdParameter(TKey id) => Id = id;

    /// <summary>Primary-key value bound to @Id / :Id depending on provider dialect.</summary>
    public TKey Id { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ForgeIdParameter<TKey> Create(TKey id) => new(id);
}

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
