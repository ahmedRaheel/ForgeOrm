using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Streaming helpers for large in-memory sequences and sample integration.
/// </summary>
public static class ForgeStreamingExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncStream<T>(
        this IEnumerable<T> rows,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
            await Task.Yield();
        }
    }

    public static async ValueTask ProcessInBatchesAsync<T>(
        this IAsyncEnumerable<T> rows,
        int batchSize,
        Func<IReadOnlyList<T>, ValueTask> processor,
        CancellationToken cancellationToken = default)
    {
        var batch = new List<T>(batchSize);

        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            batch.Add(row);

            if (batch.Count >= batchSize)
            {
                await processor(batch);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await processor(batch);
        }
    }
}
