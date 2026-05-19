namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Provider-neutral split query plan.
/// </summary>
public sealed class ForgeSplitQueryPlan
{
    public required ForgeSqlQuery ParentQuery { get; init; }
    public IReadOnlyList<ForgeSqlQuery> ChildQueries { get; init; } = Array.Empty<ForgeSqlQuery>();
}
