using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Agents;

public sealed record ForgeAgentResult(string Agent, string Summary, IReadOnlyList<string> Actions, IReadOnlyList<string> Warnings);
