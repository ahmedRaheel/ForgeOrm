using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed class RuleBasedForgeAiOptimizer : IForgeAiOptimizer
{
    /// <summary>
    /// Executes the Optimize operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the Optimize operation.</returns>
    public ForgeAiOptimizationResult Optimize(ForgeAiOptimizationRequest request)
    {
        var sql = request.Sql.Trim();
        var upper = sql.ToUpperInvariant();
        var issues = new List<string>();
        var suggestions = new List<string>();
        var indexes = new List<string>();

        if (upper.Contains("SELECT *")) { issues.Add("SELECT * detected."); suggestions.Add("Project only required columns to reduce IO and network cost."); }
        if (upper.Contains("LIKE '%")) { issues.Add("Leading wildcard LIKE detected."); suggestions.Add("Consider full-text search or a search index."); }
        if (upper.Contains("ORDER BY") && !upper.Contains("OFFSET") && !upper.Contains("TOP")) suggestions.Add("Consider pagination for ordered large result sets.");
        if (upper.Contains(" WHERE ")) indexes.Add("Review filtered columns and create covering indexes for WHERE + ORDER BY columns.");
        if (upper.Contains("JOIN")) indexes.Add("Ensure foreign-key join columns are indexed on both sides where appropriate.");

        return new ForgeAiOptimizationResult(issues, suggestions, indexes, sql);
    }
}
