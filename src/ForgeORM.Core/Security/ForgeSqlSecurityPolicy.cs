using System.Text.RegularExpressions;

namespace ForgeORM.Core.Security;

public sealed class ForgeSqlSecurityPolicy
{
    public bool AllowMultipleStatements { get; init; }
    public bool AllowDangerousCommands { get; init; }
    public IReadOnlySet<string> AllowedTables { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> AllowedColumns { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
}
