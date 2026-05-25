using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public sealed record ForgeWorkflowStep(string Name, string Kind, string Action, int RetryCount = 3, string? CompensationAction = null);
