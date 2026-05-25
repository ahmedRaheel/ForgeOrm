using Microsoft.Extensions.DependencyInjection;
using ForgeORM.Telemetry;

namespace ForgeORM.Observability.AI;

public sealed record ForgeObservabilityInsight(string Severity, string Title, string Recommendation);
