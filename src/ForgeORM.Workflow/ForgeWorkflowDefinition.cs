using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public sealed record ForgeWorkflowDefinition(string Name, IReadOnlyList<ForgeWorkflowStep> Steps);
