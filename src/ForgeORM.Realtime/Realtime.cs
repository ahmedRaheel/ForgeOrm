using System.Collections.Concurrent;
using System.Threading.Channels;
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
