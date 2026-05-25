using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public sealed record ForgeWorkflowDefinition(string Name, IReadOnlyList<ForgeWorkflowStep> Steps);
public sealed record ForgeWorkflowStep(string Name, string Kind, string Action, int RetryCount = 3, string? CompensationAction = null);
public sealed record ForgeWorkflowExecution(string Id, string WorkflowName, string Status, IReadOnlyList<ForgeWorkflowStepResult> Results);
public sealed record ForgeWorkflowStepResult(string Step, string Status, string? Error = null);
public sealed record VisualWorkflowNode(string Id, string Label, string Kind, double X, double Y);
public sealed record VisualWorkflowEdge(string From, string To, string Label = "next");
public sealed record VisualWorkflowDesignerModel(IReadOnlyList<VisualWorkflowNode> Nodes, IReadOnlyList<VisualWorkflowEdge> Edges);

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

public static class ForgeWorkflowServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeWorkflow operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeWorkflow operation.</returns>
    public static IServiceCollection AddForgeWorkflow(this IServiceCollection services) => services.AddSingleton<IForgeWorkflowEngine, ForgeWorkflowEngine>();
}
