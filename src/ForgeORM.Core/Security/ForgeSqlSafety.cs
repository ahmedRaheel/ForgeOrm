namespace ForgeORM.Core.Security;

/// <summary>
/// SQL-builder safety guard. Intended for builder-generated SQL, not arbitrary DBA-authored raw SQL.
/// </summary>
public static class ForgeSqlSafety
{
    private static readonly string[] DangerousTokens =
    {
        ";--", "/*", "*/", " xp_", " sp_executesql ", " drop ", " truncate ", " alter ", " create login ", " openrowset "
    };

    public static void GuardBuilderFragment(string fragment, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(fragment)) return;
        var text = " " + fragment + " ";
        for (var i = 0; i < DangerousTokens.Length; i++)
        {
            if (text.Contains(DangerousTokens[i], StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Unsafe SQL fragment detected in {argumentName}. Use parameters or a typed builder expression.");
        }
    }
}
