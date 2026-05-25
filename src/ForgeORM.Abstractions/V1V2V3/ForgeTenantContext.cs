namespace ForgeORM.Abstractions;

public sealed record ForgeTenantContext(
    string TenantId,
    string? ConnectionString = null,
    string? Schema = null);
