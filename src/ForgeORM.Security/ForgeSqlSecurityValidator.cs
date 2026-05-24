namespace ForgeORM.Security;

public sealed class ForgeSqlSecurityValidator : IForgeSqlSecurityValidator
{
    private static readonly string[] DangerousTokens = ["DROP ", "TRUNCATE ", "EXEC ", "EXECUTE ", "xp_", "sp_configure", "--", "/*", "*/"];
    private static readonly string[] DdlTokens = ["CREATE ", "ALTER ", "DROP "];

    /// <summary>
    /// Executes the Validate operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="allowDdl">The allowDdl value.</param>
    /// <param name="allowDangerous">The allowDangerous value.</param>
    /// <returns>The result of the Validate operation.</returns>
    public ForgeSqlSafetyResult Validate(string sql, bool allowDdl = false, bool allowDangerous = false)
    {
        var upper = $" {sql.ToUpperInvariant()} ";
        var violations = new List<string>();
        if (!allowDangerous)
            violations.AddRange(DangerousTokens.Where(token => upper.Contains(token)).Select(token => $"Dangerous SQL token detected: {token.Trim()}"));
        if (!allowDdl)
            violations.AddRange(DdlTokens.Where(token => upper.Contains(token)).Select(token => $"DDL token requires explicit permission: {token.Trim()}"));
        if (upper.Count(c => c == ';') > 1) violations.Add("Multiple SQL statements are not allowed by default.");
        return new ForgeSqlSafetyResult(violations.Count == 0, violations);
    }
}
