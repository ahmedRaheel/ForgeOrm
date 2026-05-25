using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeBackgroundJobQueue
{
    private readonly ConcurrentQueue<ForgeBackgroundJob> _jobs = new();

    public ForgeBackgroundJob Enqueue(string name)
    {
        var job = new ForgeBackgroundJob(Guid.NewGuid().ToString("N"), name, "Queued", DateTimeOffset.UtcNow);
        _jobs.Enqueue(job);
        return job;
    }

    public IReadOnlyList<ForgeBackgroundJob> Snapshot() => _jobs.ToArray();
}
