using System.Collections.Concurrent;
using System.Threading.Channels;
using ForgeORM.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Realtime;

public sealed record ForgeRealtimeEvent(string Topic, string EventName, object Payload, DateTimeOffset TimestampUtc);

public interface IForgeRealtimeHub
{
    ValueTask PublishAsync(ForgeRealtimeEvent evt, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ForgeRealtimeEvent> SubscribeAsync(string topic, CancellationToken cancellationToken = default);
}

public sealed class InMemoryForgeRealtimeHub : IForgeRealtimeHub
{
    private readonly ConcurrentDictionary<string, Channel<ForgeRealtimeEvent>> _topics = new();

    public ValueTask PublishAsync(ForgeRealtimeEvent evt, CancellationToken cancellationToken = default)
    {
        var channel = _topics.GetOrAdd(evt.Topic, _ => Channel.CreateUnbounded<ForgeRealtimeEvent>());
        return channel.Writer.WriteAsync(evt, cancellationToken);
    }

    public async IAsyncEnumerable<ForgeRealtimeEvent> SubscribeAsync(string topic, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = _topics.GetOrAdd(topic, _ => Channel.CreateUnbounded<ForgeRealtimeEvent>());
        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken)) yield return evt;
    }
}

public static class ForgeRealtimeServiceCollectionExtensions
{
    public static IServiceCollection AddForgeRealtime(this IServiceCollection services) => services.AddSingleton<IForgeRealtimeHub, InMemoryForgeRealtimeHub>();
}
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
