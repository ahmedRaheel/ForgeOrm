using System.Text.RegularExpressions;

namespace ForgeORM.Core.Security;

public sealed record ForgeSecurityValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed class ForgeSqlSecurityPolicy
{
    public bool AllowMultipleStatements { get; init; }
    public bool AllowDangerousCommands { get; init; }
    public IReadOnlySet<string> AllowedTables { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AllowedColumns { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}

public static class ForgeSqlSecurityHardener
{
    private static readonly string[] Dangerous =
    [
        "DROP DATABASE",
        "DROP TABLE",
        "TRUNCATE TABLE",
        "ALTER LOGIN",
        "xp_cmdshell",
        "sp_configure",
        "EXEC(",
        "EXECUTE("
    ];

    public static ForgeSecurityValidationResult Validate(string sql, ForgeSqlSecurityPolicy? policy = null)
    {
        policy ??= new ForgeSqlSecurityPolicy();
        var errors = new List<string>();
        var warnings = new List<string>();

        if (!policy.AllowMultipleStatements && Regex.IsMatch(sql, @";\s*\S", RegexOptions.CultureInvariant))
        {
            errors.Add("Multiple SQL statements are not allowed by policy.");
        }

        if (!policy.AllowDangerousCommands)
        {
            foreach (var word in Dangerous)
            {
                if (sql.Contains(word, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"Dangerous SQL command detected: {word}");
                }
            }
        }

        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("SELECT * is discouraged for production queries.");
        }

        if (Regex.IsMatch(sql, @"\bWHERE\b\s+1\s*=\s*1", RegexOptions.IgnoreCase))
        {
            warnings.Add("WHERE 1=1 detected. Validate dynamic SQL generation.");
        }

        return new ForgeSecurityValidationResult(errors.Count == 0, errors, warnings);
    }

    public static string MaskPii(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        if (value.Contains('@'))
        {
            var parts = value.Split('@');
            return parts[0].Length <= 2 ? "***@" + parts[1] : parts[0][..2] + "***@" + parts[1];
        }

        return value.Length <= 4 ? "****" : value[..2] + new string('*', value.Length - 4) + value[^2..];
    }
}

public sealed record ForgeTenantIsolationValidation(
    bool IsIsolated,
    string Message);

public static class ForgeTenantIsolationGuard
{
    public static ForgeTenantIsolationValidation ValidateQuery(string sql, string tenantColumn = "TenantId")
    {
        var hasTenantFilter = sql.Contains(tenantColumn, StringComparison.OrdinalIgnoreCase);
        return hasTenantFilter
            ? new ForgeTenantIsolationValidation(true, "Tenant isolation filter detected.")
            : new ForgeTenantIsolationValidation(false, $"Missing tenant isolation filter: {tenantColumn}.");
    }
}
