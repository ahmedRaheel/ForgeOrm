namespace ForgeORM.Abstractions;

public sealed record ForgeAiQueryRequest(
    string Prompt,
    string Provider,
    string? EntityName = null,
    string? Schema = null,
    string? SafetyPolicy = null);
