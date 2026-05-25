namespace ForgeORM.Abstractions;

public sealed record ForgeCompiledQueryKey(
    string Provider,
    string EntityName,
    string Shape,
    string? TenantId = null);
