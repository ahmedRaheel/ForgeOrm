using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public static class ForgeParallelQueryEngine
{
    public static async ValueTask<IReadOnlyList<TResult>> ExecuteAsync<T, TResult>(
        IEnumerable<T> source,
        ForgeParallelQueryOptions options,
        Func<T, CancellationToken, ValueTask<TResult>> worker,
        CancellationToken cancellationToken = default)
    {
        using var semaphore = new SemaphoreSlim(options.MaxDegreeOfParallelism);
        var results = new ConcurrentBag<TResult>();

        var tasks = source.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                results.Add(await worker(item, cancellationToken));
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToList();
    }
}
