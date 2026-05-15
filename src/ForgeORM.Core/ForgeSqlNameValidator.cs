using System.Text.RegularExpressions;

namespace ForgeORM.Core;

public static class ForgeSqlNameValidator
{
    private static readonly Regex SafeName = new(
        @"^[A-Za-z_][A-Za-z0-9_]*$",
        RegexOptions.Compiled);

    public static bool IsSafeIdentifier(string name)
        => !string.IsNullOrWhiteSpace(name) && SafeName.IsMatch(name);

    public static string EscapeIdentifier(string name)
    {
        if (!IsSafeIdentifier(name))
            throw new ArgumentException($"Invalid SQL identifier: {name}", nameof(name));

        return $"[{name}]";
    }
}
