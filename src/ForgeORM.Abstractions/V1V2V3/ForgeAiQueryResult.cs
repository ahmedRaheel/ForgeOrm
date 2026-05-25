namespace ForgeORM.Abstractions;

public sealed record ForgeAiQueryResult(
    string Sql,
    string Explanation,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> SuggestedIndexes);
