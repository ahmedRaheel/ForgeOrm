using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeSyncEngine
{
    private readonly ConcurrentQueue<ForgeSyncChange> _changes = new();

    public void Record(ForgeSyncChange change) => _changes.Enqueue(change);
    public IReadOnlyList<ForgeSyncChange> Pending() => _changes.ToArray();
}
