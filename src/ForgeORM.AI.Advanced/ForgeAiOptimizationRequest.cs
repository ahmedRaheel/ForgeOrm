using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed record ForgeAiOptimizationRequest(string Sql, string Provider = "SqlServer", string? ExecutionPlan = null);
