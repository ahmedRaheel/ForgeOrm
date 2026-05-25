using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed record FederatedPlanResult
{
    public required string PlanId { get; init; }
    public IReadOnlyList<FederatedExecutionStep> Steps { get; init; } = [];
    public string? OptimizedQuery { get; init; }
    public double EstimatedCost { get; init; }
    public TimeSpan EstimatedDuration { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
