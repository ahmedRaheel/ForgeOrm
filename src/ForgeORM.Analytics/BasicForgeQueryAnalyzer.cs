using ForgeORM.Abstractions;

namespace ForgeORM.Analytics;

public sealed class BasicForgeQueryAnalyzer : IForgeQueryAnalyzer
{
    /// <summary>
    /// Initializes or executes the Analyze operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The operation result.</returns>
    public ForgeQueryAnalysis Analyze(string sql)
    {
        var result = new ForgeQueryAnalysis();
        if (string.IsNullOrWhiteSpace(sql)) { result.Errors.Add("SQL is empty."); return result; }
        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase)) { result.Warnings.Add("SELECT * detected."); result.Suggestions.Add("Select explicit columns."); }
        if (sql.Contains("NOLOCK", StringComparison.OrdinalIgnoreCase)) { result.Warnings.Add("NOLOCK detected."); result.Suggestions.Add("Avoid NOLOCK unless dirty reads are acceptable."); }
        if (sql.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) && !sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase)) result.Warnings.Add("UPDATE without WHERE detected.");
        if (sql.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase) && !sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase)) result.Warnings.Add("DELETE without WHERE detected.");
        return result;
    }
}
