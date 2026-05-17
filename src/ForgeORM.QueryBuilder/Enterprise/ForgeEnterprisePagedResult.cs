namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Represents a paged query result.
/// </summary>
/// <typeparam name="T">The result item type.</typeparam>
public sealed class ForgeEnterprisePagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required long TotalCount { get; init; }
    public long TotalPages => PageSize <= 0 ? 0 : (long)Math.Ceiling(TotalCount / (double)PageSize);
}
