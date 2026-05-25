using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed record FederatedDataSource
{
    public required string Name { get; init; }
    public required FederatedSourceType Type { get; init; }
    public string? ConnectionString { get; init; }
    public string? Database { get; init; }
    public string? Schema { get; init; }
    public string? Collection { get; init; }
    public bool ReadOnly { get; init; } = true;
    public int Priority { get; init; } = 1;
}
