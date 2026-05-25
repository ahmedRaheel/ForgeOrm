using Microsoft.Extensions.DependencyInjection;
using ForgeORM.Telemetry;

namespace ForgeORM.Observability.AI;

public static class ForgeAiObservabilityServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeAiObservability operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeAiObservability operation.</returns>
    public static IServiceCollection AddForgeAiObservability(this IServiceCollection services) => services.AddSingleton<IForgeAiObservabilityAnalyzer, ForgeAiObservabilityAnalyzer>();
}
