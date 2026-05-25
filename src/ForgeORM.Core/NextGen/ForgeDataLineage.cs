using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public static class ForgeDataLineage
{
    private static readonly ConcurrentQueue<ForgeLineageEvent> Events = new();

    public static void Record(ForgeLineageEvent @event) => Events.Enqueue(@event);
    public static IReadOnlyList<ForgeLineageEvent> Snapshot() => Events.ToArray();
}
