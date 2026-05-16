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

    /// <summary>
    /// Initializes or executes the PublishAsync operation.
    /// </summary>
    /// <param name="evt">The evt value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public ValueTask PublishAsync(ForgeRealtimeEvent evt, CancellationToken cancellationToken = default)
    {
        var channel = _topics.GetOrAdd(evt.Topic, _ => Channel.CreateUnbounded<ForgeRealtimeEvent>());
        return channel.Writer.WriteAsync(evt, cancellationToken);
    }

    /// <summary>
    /// Initializes or executes the SubscribeAsync operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async IAsyncEnumerable<ForgeRealtimeEvent> SubscribeAsync(string topic, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = _topics.GetOrAdd(topic, _ => Channel.CreateUnbounded<ForgeRealtimeEvent>());
        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken)) yield return evt;
    }
}

public static class ForgeRealtimeServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeRealtime operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Initializes or executes the Id.ToString operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public string AggregateId => Id.ToString("N");
    public DateTimeOffset OccurredUtc => CreatedAt;
}
