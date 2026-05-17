namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Entry point for dynamic enterprise searches.
/// </summary>
public static class ForgeSearchQuery
{
    /// <summary>
    /// Creates a query for an entity.
    /// </summary>
    public static ForgeEnterpriseQuery<T> For<T>() => new();
}
