using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed record VirtualDataSource(string Name, string Kind, string ConnectionName);
public sealed record FederatedQuery(string Name, string Query, IReadOnlyList<string> Sources);
public sealed record FederatedQueryPlan(IReadOnlyList<string> SourcePlans, string MergeStrategy, string SqlPreview);

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

public enum FederatedSourceType
{
    SqlServer = 1,
    PostgreSql = 2,
    MySql = 3,
    Oracle = 4,
    MongoDb = 5,
    CosmosDb = 6,
    ElasticSearch = 7,
    Redis = 8,
    Api = 9,
    VectorStore = 10,
    File = 11
}

public enum FederatedExecutionMode
{
    Sequential = 1,
    Parallel = 2,
    Distributed = 3
}

public sealed record FederatedPlanResult
{
    public required string PlanId { get; init; }
    public IReadOnlyList<FederatedExecutionStep> Steps { get; init; } = [];
    public string? OptimizedQuery { get; init; }
    public double EstimatedCost { get; init; }
    public TimeSpan EstimatedDuration { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed record FederatedExecutionStep
{
    public required string Source { get; init; }
    public required string Query { get; init; }
    public FederatedExecutionMode Mode { get; init; }
    public int Order { get; init; }
}

public interface IForgeFederatedQueryPlanner
{
    FederatedQueryPlan Plan(FederatedQuery query, IReadOnlyList<VirtualDataSource> sources);
    FederatedPlanResult Plan(string query, IReadOnlyList<FederatedDataSource> sources);
    FederatedPlanResult Plan(FederatedPlanRequest request);
}

public sealed class ForgeFederatedQueryPlanner : IForgeFederatedQueryPlanner
{
    public FederatedQueryPlan Plan(FederatedQuery query, IReadOnlyList<VirtualDataSource> sources)
    {
        var sourcePlans = query.Sources.Select(s => $"Route '{query.Query}' to {s}").ToList();
        return new FederatedQueryPlan(sourcePlans, "Union/Merge by compatible columns", $"-- Federated query: {query.Name}\n{query.Query}");
    }

    public FederatedPlanResult Plan(string query, IReadOnlyList<FederatedDataSource> sources)
        => Plan(new FederatedPlanRequest { Query = query, Sources = sources });

    public FederatedPlanResult Plan(FederatedPlanRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Query);

        var orderedSources = request.Sources
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToList();

        var steps = orderedSources.Count == 0
            ? [new FederatedExecutionStep { Source = "default", Query = request.Query, Mode = request.ExecutionMode, Order = 1 }]
            : orderedSources.Select((source, index) => new FederatedExecutionStep
            {
                Source = source.Name,
                Query = BuildSourceQuery(request.Query, source),
                Mode = request.ExecutionMode,
                Order = index + 1
            }).ToList();

        var warnings = new List<string>();
        if (request.TimeoutSeconds < 30) warnings.Add("Timeout is low for a federated query.");
        if (!request.EnableSecurityValidation) warnings.Add("Security validation is disabled.");
        if (orderedSources.Any(x => !x.ReadOnly)) warnings.Add("One or more sources are writable; validate commands carefully.");

        return new FederatedPlanResult
        {
            PlanId = $"fp-{Guid.NewGuid():N}",
            Steps = steps,
            OptimizedQuery = request.EnableOptimization ? NormalizeQuery(request.Query) : request.Query,
            EstimatedCost = Math.Max(1, steps.Count) * (request.ExecutionMode == FederatedExecutionMode.Parallel ? 0.75 : 1.25),
            EstimatedDuration = TimeSpan.FromMilliseconds(150 * Math.Max(1, steps.Count)),
            Warnings = warnings
        };
    }

    private static string BuildSourceQuery(string query, FederatedDataSource source)
    {
        var prefix = source.Type switch
        {
            FederatedSourceType.SqlServer => "-- SQL Server source",
            FederatedSourceType.PostgreSql => "-- PostgreSQL source",
            FederatedSourceType.MySql => "-- MySQL source",
            FederatedSourceType.Oracle => "-- Oracle source",
            FederatedSourceType.MongoDb => "// MongoDB source",
            FederatedSourceType.Api => "// API source",
            FederatedSourceType.VectorStore => "// Vector source",
            _ => "-- Federated source"
        };

        return $"{prefix}: {source.Name}\n{query}";
    }

    private static string NormalizeQuery(string query)
        => string.Join(' ', query.Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

public static class ForgeDataVirtualizationServiceCollectionExtensions
{
    public static IServiceCollection AddForgeDataVirtualization(this IServiceCollection services) => services.AddSingleton<IForgeFederatedQueryPlanner, ForgeFederatedQueryPlanner>();
}
