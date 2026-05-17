namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Options for split parent-child query loading.
/// </summary>
public sealed class ForgeSplitQueryOptions
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int BatchSize { get; set; } = 500;
    public bool KeepParentOrder { get; set; } = true;
}
