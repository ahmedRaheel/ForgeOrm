namespace ForgeORM.Security;

public sealed record ForgeSqlSafetyResult(bool IsSafe, IReadOnlyList<string> Violations);
