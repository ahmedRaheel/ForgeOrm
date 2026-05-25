using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public sealed record ForgeWorkflowExecution(string Id, string WorkflowName, string Status, IReadOnlyList<ForgeWorkflowStepResult> Results);
