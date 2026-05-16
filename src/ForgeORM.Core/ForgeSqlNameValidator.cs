using System.Text.RegularExpressions;

namespace ForgeORM.Core;

public static class ForgeSqlNameValidator
{
    private static readonly Regex SafeName = new(
        @"^[A-Za-z_][A-Za-z0-9_]*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Initializes or executes the IsSafeIdentifier operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The operation result.</returns>
    public static bool IsSafeIdentifier(string name)
        => !string.IsNullOrWhiteSpace(name) && SafeName.IsMatch(name);

    /// <summary>
    /// Initializes or executes the EscapeIdentifier operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The operation result.</returns>
    public static string EscapeIdentifier(string name)
    {
        if (!IsSafeIdentifier(name))
            throw new ArgumentException($"Invalid SQL identifier: {name}", nameof(name));

        return $"[{name}]";
    }
}
