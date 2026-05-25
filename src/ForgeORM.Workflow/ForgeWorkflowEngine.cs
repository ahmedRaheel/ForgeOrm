using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public sealed class ForgeWorkflowEngine : IForgeWorkflowEngine
{
    /// <summary>
    /// Executes the RunAsync operation.
    /// </summary>
    /// <param name="workflow">The workflow value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RunAsync operation.</returns>
    public async ValueTask<ForgeWorkflowExecution> RunAsync(ForgeWorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        var results = new List<ForgeWorkflowStepResult>();
        foreach (var step in workflow.Steps)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken);
                results.Add(new ForgeWorkflowStepResult(step.Name, "Completed"));
            }
            catch (Exception ex)
            {
                results.Add(new ForgeWorkflowStepResult(step.Name, "Failed", ex.Message));
                return new ForgeWorkflowExecution(Guid.NewGuid().ToString("N"), workflow.Name, "Failed", results);
            }
        }
        return new ForgeWorkflowExecution(Guid.NewGuid().ToString("N"), workflow.Name, "Completed", results);
    }

    /// <summary>
    /// Executes the ToDesignerModel operation.
    /// </summary>
    /// <param name="workflow">The workflow value.</param>
    /// <returns>The result of the ToDesignerModel operation.</returns>
    public VisualWorkflowDesignerModel ToDesignerModel(ForgeWorkflowDefinition workflow)
    {
        var nodes = workflow.Steps.Select((s, i) => new VisualWorkflowNode(s.Name, s.Name, s.Kind, 120 + i * 220, 120)).ToList();
        var edges = nodes.Zip(nodes.Skip(1), (a, b) => new VisualWorkflowEdge(a.Id, b.Id)).ToList();
        return new VisualWorkflowDesignerModel(nodes, edges);
    }
}
