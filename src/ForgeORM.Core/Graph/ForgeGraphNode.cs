using System.Reflection;

namespace ForgeORM.Core.Graph;

/// <summary>
/// Represents one table-level node in a graph persistence plan.
/// </summary>
public sealed class ForgeGraphNode
{
    /// <summary>
    /// Gets the table name targeted by this node.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Gets the entity CLR type.
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// Gets the graph depth of this node.
    /// </summary>
    public required int Depth { get; init; }

    /// <summary>
    /// Gets the rows represented by this node.
    /// </summary>
    public List<object> Rows { get; } = [];

    /// <summary>
    /// Gets or sets the parent CLR type when this node represents children.
    /// </summary>
    public Type? ParentType { get; set; }

    /// <summary>
    /// Gets or sets the parent key property.
    /// </summary>
    public PropertyInfo? ParentKeyProperty { get; set; }

    /// <summary>
    /// Gets or sets the child foreign-key property.
    /// </summary>
    public PropertyInfo? ForeignKeyProperty { get; set; }

    /// <summary>
    /// Gets the parent entity instance for each child row.
    /// </summary>
    public Dictionary<object, object> ParentByChild { get; } = new(ReferenceEqualityComparer.Instance);
}
