using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Agents;

public sealed record ForgeAgentTask(string Goal, IReadOnlyDictionary<string,string>? Context = null);
public sealed record ForgeAgentResult(string Agent, string Summary, IReadOnlyList<string> Actions, IReadOnlyList<string> Warnings);

public interface IForgeAiAgent
{
    string Name { get; }
    Task<ForgeAgentResult> RunAsync(ForgeAgentTask task, CancellationToken cancellationToken = default);
}

public sealed class ForgeOptimizationAgent : IForgeAiAgent
{
    public string Name => "OptimizationAgent";
    public Task<ForgeAgentResult> RunAsync(ForgeAgentTask task, CancellationToken cancellationToken = default)
    {
        var actions = new[] { "Analyze slow SQL", "Recommend covering indexes", "Check cache candidates", "Check tenant filters" };
        return Task.FromResult(new ForgeAgentResult(Name, $"Optimization plan created for: {task.Goal}", actions, []));
    }
}

public sealed class ForgeAgentRunner(IEnumerable<IForgeAiAgent> agents)
{
    public async Task<IReadOnlyList<ForgeAgentResult>> RunAllAsync(ForgeAgentTask task, CancellationToken cancellationToken = default)
    {
        var results = new List<ForgeAgentResult>();
        foreach (var agent in agents) results.Add(await agent.RunAsync(task, cancellationToken));
        return results;
    }
}

public static class ForgeAiAgentsServiceCollectionExtensions
{
    public static IServiceCollection AddForgeAiAgents(this IServiceCollection services)
    {
        services.AddSingleton<IForgeAiAgent, ForgeOptimizationAgent>();
        services.AddSingleton<ForgeAgentRunner>();
        return services;
    }
}
