using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public sealed record ForgeWorkflowStepResult(string Step, string Status, string? Error = null);
