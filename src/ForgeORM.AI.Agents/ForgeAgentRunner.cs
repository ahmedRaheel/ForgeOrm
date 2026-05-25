using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Agents;

public sealed class ForgeAgentRunner(IEnumerable<IForgeAiAgent> agents)
{
    /// <summary>
    /// Executes the RunAllAsync operation.
    /// </summary>
    /// <param name="task">The task value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RunAllAsync operation.</returns>
    public async ValueTask<IReadOnlyList<ForgeAgentResult>> RunAllAsync(ForgeAgentTask task, CancellationToken cancellationToken = default)
    {
        var results = new List<ForgeAgentResult>();
        foreach (var agent in agents) results.Add(await agent.RunAsync(task, cancellationToken));
        return results;
    }
}
