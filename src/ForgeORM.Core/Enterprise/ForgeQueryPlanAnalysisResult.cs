using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Query plan analysis result.
/// </summary>
public sealed record ForgeQueryPlanAnalysisResult(
    string Sql,
    IReadOnlyList<ForgeQueryPlanWarning> Warnings,
    IReadOnlyList<string> SuggestedIndexes,
    IReadOnlyList<string> OptimizationHints);
