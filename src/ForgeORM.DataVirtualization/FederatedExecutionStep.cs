using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed record FederatedExecutionStep
{
    public required string Source { get; init; }
    public required string Query { get; init; }
    public FederatedExecutionMode Mode { get; init; }
    public int Order { get; init; }
}
