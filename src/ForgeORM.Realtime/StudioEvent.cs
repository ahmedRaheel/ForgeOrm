using System.Collections.Concurrent;
using System.Threading.Channels;
using ForgeORM.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Realtime;

public sealed record StudioEvent : IForgeEvent
{
    public required Guid Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public object? Payload { get; init; }
    public string? TenantId { get; init; }
    public string? UserId { get; init; }
    public string? Severity { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public string AggregateId => Id.ToString("N");
    public DateTimeOffset OccurredUtc => CreatedAt;
}
