using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Workflow;

public sealed record VisualWorkflowDesignerModel(IReadOnlyList<VisualWorkflowNode> Nodes, IReadOnlyList<VisualWorkflowEdge> Edges);
