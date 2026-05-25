using Microsoft.Extensions.DependencyInjection;
using ForgeORM.Telemetry;

namespace ForgeORM.Observability.AI;

public interface IForgeAiObservabilityAnalyzer
/// <summary>
/// Defines the Analyze operation.
/// </summary>
/// <param name="snapshot">The snapshot value.</param>
/// <returns>The result of the Analyze operation.</returns>
{
    /// <summary>
    /// Defines the Analyze operation.
    /// </summary>
    /// <param name="snapshot">The snapshot value.</param>
    /// <returns>The result of the Analyze operation.</returns>
    IReadOnlyList<ForgeObservabilityInsight> Analyze(ForgeMonitoringSnapshot snapshot);
}
