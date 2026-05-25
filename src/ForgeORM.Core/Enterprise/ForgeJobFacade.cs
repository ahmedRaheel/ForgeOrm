using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeJobFacade
{
    private readonly ForgeDb _db;
    internal ForgeJobFacade(ForgeDb db) => _db = db;
    public ValueTask<ForgeJobEnqueueResult> EnqueueAsync<TJob>(TJob job, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(new ForgeJobEnqueueResult(Guid.NewGuid().ToString("N"), "Queued"));
}
