namespace ForgeORM.Core;

public sealed record ForgeRuntimeOutboxMessage(Guid Id, string Type, string Payload, DateTimeOffset CreatedAtUtc, string? TenantId = null);
