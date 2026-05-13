namespace ForgeORM.Analytics;

public sealed record ForgeEnterpriseAnalysis(
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> SuggestedIndexes,
    IReadOnlyList<string> OptimizationHints);

public sealed class ForgeEnterpriseAnalyzer
{
    public ForgeEnterpriseAnalysis AnalyzeSql(string sql)
    {
        var warnings = new List<string>();
        var indexes = new List<string>();
        var hints = new List<string>();

        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
            warnings.Add("Avoid SELECT * for high-traffic endpoints. Select only required columns.");

        if (sql.Contains("LIKE '%", StringComparison.OrdinalIgnoreCase))
            warnings.Add("Leading wildcard LIKE can prevent index usage.");

        if (sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase) &&
            !sql.Contains("OFFSET", StringComparison.OrdinalIgnoreCase) &&
            !sql.Contains("LIMIT", StringComparison.OrdinalIgnoreCase))
            hints.Add("Consider adding pagination for large result sets.");

        if (sql.Contains("WHERE IsActive", StringComparison.OrdinalIgnoreCase))
            indexes.Add("Consider an index on IsActive if this filter is frequently used.");

        if (!sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
            hints.Add("No WHERE clause found. Confirm this is intentional for large tables.");

        return new ForgeEnterpriseAnalysis(warnings, indexes, hints);
    }
}
