namespace ForgeORM.Abstractions;

public sealed record ForgeOutboxMessage(
    Guid Id,
    string EventType,
    string Payload,
    DateTimeOffset CreatedAt,
    string? TenantId = null,
    DateTimeOffset? ProcessedAt = null);
