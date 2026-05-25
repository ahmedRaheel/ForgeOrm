using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public static class ForgeStreamingExtensions
{
    public static async IAsyncEnumerable<IReadOnlyList<T>> StreamBatchesAsync<T>(
        this IForgeQuery<T> query,
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
        var batch = new List<T>(batchSize);
        await foreach (var row in query.StreamAsync(cancellationToken).ConfigureAwait(false))
        {
            batch.Add(row);
            if (batch.Count < batchSize) continue;
            yield return batch.ToArray();
            batch.Clear();
        }
        if (batch.Count > 0)
            yield return batch.ToArray();
    }
}
