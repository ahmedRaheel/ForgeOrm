using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Agents;

public static class ForgeAiAgentsServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeAiAgents operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeAiAgents operation.</returns>
    public static IServiceCollection AddForgeAiAgents(this IServiceCollection services)
    {
        services.AddSingleton<IForgeAiAgent, ForgeOptimizationAgent>();
        services.AddSingleton<ForgeAgentRunner>();
        return services;
    }
}
