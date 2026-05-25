using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeIntelligentConnectionManager
{
    private int _active;
    private int _waiting;

    public IDisposable Acquire()
    {
        Interlocked.Increment(ref _active);
        return new ReleaseHandle(() => Interlocked.Decrement(ref _active));
    }

    public ForgeConnectionPoolSnapshot Snapshot()
        => new(_active, Math.Max(0, Environment.ProcessorCount * 4 - _active), _waiting, DateTimeOffset.UtcNow);

    private sealed class ReleaseHandle : IDisposable
    {
        private readonly Action _release;
        private int _disposed;
        public ReleaseHandle(Action release) => _release = release;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) _release();
        }
    }
}
