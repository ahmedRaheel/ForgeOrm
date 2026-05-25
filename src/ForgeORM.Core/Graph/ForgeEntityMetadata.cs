using System.Reflection;

namespace ForgeORM.Core.Graph;

/// <summary>
/// Cached entity metadata used by graph persistence.
/// </summary>
public sealed class ForgeEntityMetadata
{
    /// <summary>
    /// Gets the CLR entity type.
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Gets the key property when discovered.
    /// </summary>
    public PropertyInfo? KeyProperty { get; init; }

    /// <summary>
    /// Gets whether the key is database generated.
    /// </summary>
    public bool IsIdentityKey { get; init; }

    /// <summary>
    /// Gets scalar mapped properties.
    /// </summary>
    public required IReadOnlyList<PropertyInfo> ScalarProperties { get; init; }

    /// <summary>
    /// Gets child collection properties.
    /// </summary>
    public required IReadOnlyList<PropertyInfo> ChildCollections { get; init; }
}
