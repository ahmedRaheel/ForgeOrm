using System.Collections.Concurrent;

namespace ForgeORM.Core;

/// <summary>
/// Enterprise concurrency and large-data helpers.
/// </summary>
public partial class ForgeDb
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> QueryThrottles = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Executes a query behind a named semaphore to protect the database under high concurrency.
    /// Use this for expensive reports, exports, and dashboard queries.
    /// </summary>
    public async ValueTask<IReadOnlyList<T>> QueryThrottledAsync<T>(
        string throttleName,
        int maxConcurrency,
        string sql,
        object? parameters = null,
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(throttleName))
        {
            throw new ArgumentException("Throttle name is required.", nameof(throttleName));
        }

        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency), "Max concurrency must be greater than zero.");
        }

        var gate = QueryThrottles.GetOrAdd(throttleName, _ => new SemaphoreSlim(maxConcurrency, maxConcurrency));
        await gate.WaitAsync(cancellationToken);

        var started = DateTimeOffset.UtcNow;

        try
        {
            var rows = await QueryAsync<T>(
                sql,
                parameters,
                timeoutSeconds,
                cancellationToken);

            ForgeEnterpriseQueryMonitor.Record(new ForgeEnterpriseQueryMetric(
                throttleName,
                sql,
                started,
                DateTimeOffset.UtcNow - started,
                rows.Count,
                Throttled: true));

            return rows;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Executes a keyset/seek page for large tables. This avoids high OFFSET cost on millions of rows.
    /// </summary>
    public ValueTask<IReadOnlyList<T>> QueryKeysetPageAsync<T, TKey>(
        string tableName,
        string keyColumn,
        TKey? afterKey,
        int take,
        string? whereSql = null,
        object? parameters = null,
        string orderDirection = "ASC",
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name is required.", nameof(tableName));
        }

        if (string.IsNullOrWhiteSpace(keyColumn))
        {
            throw new ArgumentException("Key column is required.", nameof(keyColumn));
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be greater than zero.");
        }

        var direction = string.Equals(orderDirection, "DESC", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

        var comparison = direction == "DESC" ? "<" : ">";

        var clauses = new List<string>();

        if (!string.IsNullOrWhiteSpace(whereSql))
        {
            clauses.Add($"({whereSql})");
        }

        var merged = ToParameterDictionary(parameters);

        if (afterKey is not null)
        {
            clauses.Add($"{keyColumn} {comparison} @ForgeAfterKey");
            merged["ForgeAfterKey"] = afterKey;
        }

        merged["ForgeTake"] = take;

        var where = clauses.Count == 0
            ? string.Empty
            : " WHERE " + string.Join(" AND ", clauses);

        var sql = $"""
SELECT TOP (@ForgeTake) *
FROM {tableName}
{where}
ORDER BY {keyColumn} {direction}
""";

        return QueryAsync<T>(
            sql,
            merged,
            timeoutSeconds,
            cancellationToken);
    }

    /// <summary>
    /// Processes a large table in keyset batches without loading all rows in memory.
    /// </summary>
    public async ValueTask<int> ProcessKeysetBatchesAsync<T, TKey>(
        string tableName,
        string keyColumn,
        Func<T, TKey> keySelector,
        Func<IReadOnlyList<T>, CancellationToken, ValueTask> processBatch,
        int batchSize = 1000,
        string? whereSql = null,
        object? parameters = null,
        string orderDirection = "ASC",
        int? timeoutSeconds = null,
        CancellationToken cancellationToken = default)
        where TKey : IComparable<TKey>
    {
        if (keySelector is null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        if (processBatch is null)
        {
            throw new ArgumentNullException(nameof(processBatch));
        }

        TKey? afterKey = default;
        var total = 0;

        while (true)
        {
            var batch = await QueryKeysetPageAsync<T, TKey>(
                tableName,
                keyColumn,
                afterKey,
                batchSize,
                whereSql,
                parameters,
                orderDirection,
                timeoutSeconds,
                cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            await processBatch(batch, cancellationToken);
            total += batch.Count;
            afterKey = keySelector(batch[^1]);

            if (batch.Count < batchSize)
            {
                break;
            }
        }

        return total;
    }

    private static Dictionary<string, object?> ToParameterDictionary(object? parameters)
    {
        if (parameters is null)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        if (parameters is IReadOnlyDictionary<string, object?> readonlyDictionary)
        {
            return new Dictionary<string, object?>(readonlyDictionary, StringComparer.OrdinalIgnoreCase);
        }

        if (parameters is IDictionary<string, object?> dictionary)
        {
            return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
        }

        return parameters
            .GetType()
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(x => x.CanRead)
            .ToDictionary(x => x.Name, x => ForgeRuntimeAccessorCache.Get(x, parameters), StringComparer.OrdinalIgnoreCase);
    }
}
