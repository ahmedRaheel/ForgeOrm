using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public sealed record VisualWorkflowNode(string Id, string Label, string Kind, double X, double Y);
