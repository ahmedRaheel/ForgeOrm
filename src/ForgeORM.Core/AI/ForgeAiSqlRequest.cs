namespace ForgeORM.Core.AI;

public sealed record ForgeAiSqlRequest(
    string Prompt,
    string? Entity = null,
    string? Provider = null,
    bool SafeMode = true);
