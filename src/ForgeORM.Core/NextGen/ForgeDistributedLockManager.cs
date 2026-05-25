using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeDistributedLockManager
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    public async ValueTask<IDisposable> AcquireAsync(string name, CancellationToken cancellationToken = default)
    {
        var gate = _locks.GetOrAdd(name, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        return new ReleaseHandle(gate);
    }

    private sealed class ReleaseHandle : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        public ReleaseHandle(SemaphoreSlim gate) => _gate = gate;
        public void Dispose() => _gate.Release();
    }
}
