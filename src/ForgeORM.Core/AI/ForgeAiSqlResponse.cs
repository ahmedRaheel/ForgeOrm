namespace ForgeORM.Core.AI;

public sealed record ForgeAiSqlResponse(
    string Sql,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> SuggestedIndexes,
    IReadOnlyList<string> Explanation);
