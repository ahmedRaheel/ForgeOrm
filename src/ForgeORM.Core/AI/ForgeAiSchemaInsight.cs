namespace ForgeORM.Core.AI;

public sealed record ForgeAiSchemaInsight(
    string Entity,
    IReadOnlyList<string> SuggestedDtos,
    IReadOnlyList<string> SuggestedEndpoints,
    IReadOnlyList<string> SuggestedIndexes);
