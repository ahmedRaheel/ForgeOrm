namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Query cache options.
/// </summary>
public sealed class ForgeQueryCacheOptions
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
    public string? Region { get; set; }
}
