namespace ForgeORM.Analytics;

public sealed record ForgeEnterpriseAnalysis(
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> SuggestedIndexes,
    IReadOnlyList<string> OptimizationHints);
