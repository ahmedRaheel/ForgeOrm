namespace ForgeORM.Core.Graph;

/// <summary>
/// Represents a compiled graph persistence plan.
/// </summary>
public sealed class ForgeGraphPlan
{
    /// <summary>
    /// Gets the operation this plan was built for.
    /// </summary>
    public required ForgeGraphOperation Operation { get; init; }

    /// <summary>
    /// Gets the graph nodes grouped by table/type.
    /// </summary>
    public List<ForgeGraphNode> Nodes { get; } = [];

    /// <summary>
    /// Gets nodes in insert execution order.
    /// </summary>
    public IEnumerable<ForgeGraphNode> GetInsertOrder() => Nodes.OrderBy(x => x.Depth);

    /// <summary>
    /// Gets nodes in delete execution order.
    /// </summary>
    public IEnumerable<ForgeGraphNode> GetDeleteOrder() => Nodes.OrderByDescending(x => x.Depth);
}
