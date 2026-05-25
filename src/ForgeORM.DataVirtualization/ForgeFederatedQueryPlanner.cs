using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.DataVirtualization;

public sealed class ForgeFederatedQueryPlanner : IForgeFederatedQueryPlanner
{
    /// <summary>
    /// Executes the Plan operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="sources">The sources value.</param>
    /// <returns>The result of the Plan operation.</returns>
    public FederatedQueryPlan Plan(FederatedQuery query, IReadOnlyList<VirtualDataSource> sources)
    {
        var sourcePlans = query.Sources.Select(s => $"Route '{query.Query}' to {s}").ToList();
        return new FederatedQueryPlan(sourcePlans, "Union/Merge by compatible columns", $"-- Federated query: {query.Name}\n{query.Query}");
    }

    /// <summary>
    /// Executes the Plan operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <param name="sources">The sources value.</param>
    /// <returns>The result of the Plan operation.</returns>
    public FederatedPlanResult Plan(string query, IReadOnlyList<FederatedDataSource> sources)
        => Plan(new FederatedPlanRequest { Query = query, Sources = sources });

    /// <summary>
    /// Executes the Plan operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the Plan operation.</returns>
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
