using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public interface IForgeWorkflowEngine
/// <summary>
/// Defines the RunAsync operation.
/// </summary>
/// <param name="workflow">The workflow value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the RunAsync operation.</returns>
{
    /// <summary>
    /// Defines the RunAsync operation.
    /// </summary>
    /// <param name="workflow">The workflow value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RunAsync operation.</returns>
    ValueTask<ForgeWorkflowExecution> RunAsync(ForgeWorkflowDefinition workflow, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ToDesignerModel operation.
    /// </summary>
    /// <param name="workflow">The workflow value.</param>
    /// <returns>The result of the ToDesignerModel operation.</returns>
    VisualWorkflowDesignerModel ToDesignerModel(ForgeWorkflowDefinition workflow);
}
