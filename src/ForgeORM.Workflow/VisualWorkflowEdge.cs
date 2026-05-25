using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public sealed record VisualWorkflowEdge(string From, string To, string Label = "next");
