using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>Query validation result.</summary>
public sealed record ForgeQueryValidationResult(bool IsValid, IReadOnlyList<string> Warnings);
