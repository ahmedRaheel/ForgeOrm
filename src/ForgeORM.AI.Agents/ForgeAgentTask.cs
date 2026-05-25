using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.AI.Agents;

public sealed record ForgeAgentTask(string Goal, IReadOnlyDictionary<string,string>? Context = null);
