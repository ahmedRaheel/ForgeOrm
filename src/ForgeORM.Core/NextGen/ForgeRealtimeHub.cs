using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeRealtimeHub
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ForgeRealtimeEvent>> _channels = new(StringComparer.OrdinalIgnoreCase);

    public void Publish(string channel, string payload)
        => _channels.GetOrAdd(channel, _ => new ConcurrentQueue<ForgeRealtimeEvent>())
            .Enqueue(new ForgeRealtimeEvent(channel, payload, DateTimeOffset.UtcNow));

    public IReadOnlyList<ForgeRealtimeEvent> Read(string channel)
        => _channels.TryGetValue(channel, out var q) ? q.ToArray() : [];
}
