namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Describes one split-query child include.
/// </summary>
public sealed class ForgeSplitInclude
{
    public required string ParentKey { get; init; }
    public required string ChildForeignKey { get; init; }
    public required string ChildTable { get; init; }
    public required string ChildProperty { get; init; }
}
