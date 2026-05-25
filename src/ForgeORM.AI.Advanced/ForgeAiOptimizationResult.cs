using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed record ForgeAiOptimizationResult(IReadOnlyList<string> Issues, IReadOnlyList<string> Suggestions, IReadOnlyList<string> IndexRecommendations, string OptimizedSql);
