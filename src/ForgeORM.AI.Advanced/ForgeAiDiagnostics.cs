using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed class ForgeAiDiagnostics : IForgeAiDiagnostics
{
    /// <summary>
    /// Executes the Diagnose operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="elapsed">The elapsed value.</param>
    /// <param name="rowCount">The rowCount value.</param>
    /// <returns>The result of the Diagnose operation.</returns>
    public ForgeAiDiagnosticResult Diagnose(string sql, TimeSpan elapsed, int rowCount)
    {
        var findings = new List<string>();
        var fixes = new List<string>();
        if (elapsed.TotalMilliseconds > 500) { findings.Add("Slow query detected."); fixes.Add("Check indexes, execution plan, pagination and projection size."); }
        if (rowCount > 5000) { findings.Add("Large result set detected."); fixes.Add("Use server-side pagination or streaming."); }
        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase)) { findings.Add("Unbounded projection detected."); fixes.Add("Select explicit columns or DTO projection."); }
        var severity = findings.Count switch { 0 => "Healthy", 1 => "Warning", _ => "Critical" };
        return new ForgeAiDiagnosticResult(severity, findings, fixes);
    }
}
