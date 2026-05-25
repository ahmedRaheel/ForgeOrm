using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

public sealed record AppendEventRequest
{
    public required string StreamId { get; init; }

    public required string EventType { get; init; }

    public required object Data { get; init; }

    public string? TenantId { get; init; }

    public string? UserId { get; init; }

    public DateTimeOffset Timestamp { get; init; }
        = DateTimeOffset.UtcNow;

    public long? ExpectedVersion { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public CancellationToken CancellationToken { get; init; }
}
