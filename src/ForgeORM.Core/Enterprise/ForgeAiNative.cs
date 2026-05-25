using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// AI-native deterministic fallback service.
/// </summary>
public static class ForgeAiNative
{
    public static string ToSql(string naturalLanguage)
    {
        if (naturalLanguage.Contains("top", StringComparison.OrdinalIgnoreCase) &&
            naturalLanguage.Contains("customers", StringComparison.OrdinalIgnoreCase))
        {
            return "SELECT TOP (10) CustomerId, SUM(GrandTotal) AS Revenue FROM Orders GROUP BY CustomerId ORDER BY Revenue DESC";
        }

        return "-- AI SQL generation extension point. Configure provider to generate SQL.";
    }

    public static IReadOnlyList<string> SuggestOptimizations(string sql)
        => ForgeQueryPlanAnalyzer.Analyze(sql).OptimizationHints;
}
