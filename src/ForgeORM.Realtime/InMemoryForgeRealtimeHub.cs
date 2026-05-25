using System.Collections.Concurrent;
using System.Threading.Channels;
using ForgeORM.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Realtime;

public sealed class InMemoryForgeRealtimeHub : IForgeRealtimeHub
{
    private readonly ConcurrentDictionary<string, Channel<ForgeRealtimeEvent>> _topics = new();

    /// <summary>
    /// Executes the PublishAsync operation.
    /// </summary>
    /// <param name="evt">The evt value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the PublishAsync operation.</returns>
    public ValueTask PublishAsync(ForgeRealtimeEvent evt, CancellationToken cancellationToken = default)
    {
        var channel = _topics.GetOrAdd(evt.Topic, _ => Channel.CreateUnbounded<ForgeRealtimeEvent>());
        return channel.Writer.WriteAsync(evt, cancellationToken);
    }

    /// <summary>
    /// Executes the SubscribeAsync operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SubscribeAsync operation.</returns>
    public async IAsyncEnumerable<ForgeRealtimeEvent> SubscribeAsync(string topic, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = _topics.GetOrAdd(topic, _ => Channel.CreateUnbounded<ForgeRealtimeEvent>());
        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken)) yield return evt;
    }
}
