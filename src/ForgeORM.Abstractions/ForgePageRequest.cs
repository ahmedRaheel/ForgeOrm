using System.Data;

namespace ForgeORM.Abstractions;

public sealed class ForgePageRequest
{
    public required string Sql { get; init; }
    public object? Parameters { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public required string OrderBy { get; init; }
    public int Skip => Math.Max(Page - 1, 0) * PageSize;
}
