using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Query plan analyzer foundation.
/// </summary>
public static class ForgeQueryPlanAnalyzer
{
    public static ForgeQueryPlanAnalysisResult Analyze(string sql)
    {
        var warnings = new List<ForgeQueryPlanWarning>();
        var hints = new List<string>();

        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new("SELECT_STAR", "Medium", "Avoid SELECT * in large enterprise queries."));
            hints.Add("Project only required columns.");
        }

        if (sql.Contains("OFFSET", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new("OFFSET_PAGING", "High", "OFFSET can become slow for millions of rows."));
            hints.Add("Use keyset pagination for large datasets.");
        }

        if (!sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new("NO_FILTER", "Medium", "Query has no WHERE clause."));
            hints.Add("Add filters or pagination before using this query in production.");
        }

        return new ForgeQueryPlanAnalysisResult(
            sql,
            warnings,
            ["Review WHERE + ORDER BY columns for composite indexes."],
            hints.Count == 0 ? ["No major optimization warnings detected."] : hints);
    }
}
