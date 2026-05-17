namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Captures query profiling information.
/// </summary>
public sealed class ForgeEnterpriseQueryProfile
{
    public required string Name { get; init; }
    public required string Sql { get; init; }
    public TimeSpan Duration { get; init; }
    public long RowsReturned { get; init; }
    public bool CacheHit { get; init; }
    public IReadOnlyList<string> IndexSuggestions { get; init; } = Array.Empty<string>();
}
