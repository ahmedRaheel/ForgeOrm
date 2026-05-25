using System.Text.RegularExpressions;

namespace ForgeORM.Core.Security;

public sealed record ForgeSecurityValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
