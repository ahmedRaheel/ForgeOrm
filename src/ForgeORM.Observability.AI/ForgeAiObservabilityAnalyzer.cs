using Microsoft.Extensions.DependencyInjection;
using ForgeORM.Telemetry;

namespace ForgeORM.Observability.AI;

public sealed class ForgeAiObservabilityAnalyzer : IForgeAiObservabilityAnalyzer
{
    /// <summary>
    /// Executes the Analyze operation.
    /// </summary>
    /// <param name="snapshot">The snapshot value.</param>
    /// <returns>The result of the Analyze operation.</returns>
    public IReadOnlyList<ForgeObservabilityInsight> Analyze(ForgeMonitoringSnapshot snapshot)
    {
        var list = new List<ForgeObservabilityInsight>();
        if (snapshot.FailedQueries > 0) list.Add(new("High", "Query failures detected", "Inspect exceptions, provider connectivity, tenant filters and parameterization."));
        if (snapshot.AverageMilliseconds > 300) list.Add(new("Medium", "Average query latency is high", "Add indexes, cache read-heavy projections, and check N+1 includes."));
        foreach (var q in snapshot.SlowQueries.Take(5)) list.Add(new("Medium", "Slow SQL", $"Review execution plan for {q.Operation}; elapsed {q.ElapsedMilliseconds}ms."));
        if (list.Count == 0) list.Add(new("Info", "Healthy", "No major telemetry issue found in current snapshot."));
        return list;
    }
}
