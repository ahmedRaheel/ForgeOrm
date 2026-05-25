using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed record FederatedPlanRequest
{
    public required string Query { get; init; }
    public IReadOnlyList<FederatedDataSource> Sources { get; init; } = [];
    public string? TenantId { get; init; }
    public string? UserId { get; init; }
    public FederatedExecutionMode ExecutionMode { get; init; } = FederatedExecutionMode.Parallel;
    public bool EnableCaching { get; init; } = true;
    public bool EnableTelemetry { get; init; } = true;
    public bool EnableOptimization { get; init; } = true;
    public bool EnableSecurityValidation { get; init; } = true;
    public int TimeoutSeconds { get; init; } = 120;
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
