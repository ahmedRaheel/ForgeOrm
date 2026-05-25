using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Agents;

public interface IForgeAiAgent
/// <summary>
/// Defines the RunAsync operation.
/// </summary>
/// <param name="task">The task value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the RunAsync operation.</returns>
{
    /// <summary>
    /// Defines the RunAsync operation.
    /// </summary>
    /// <param name="task">The task value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RunAsync operation.</returns>
    string Name { get; }
    /// <summary>
    /// Defines the RunAsync operation.
    /// </summary>
    /// <param name="task">The task value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RunAsync operation.</returns>
    ValueTask<ForgeAgentResult> RunAsync(ForgeAgentTask task, CancellationToken cancellationToken = default);
}
