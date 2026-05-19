namespace ForgeORM.Core.Reliability;

public sealed record ForgeRetryPolicy(
    int MaxRetries = 3,
    TimeSpan? InitialDelay = null,
    bool RetryDeadlocks = true,
    bool RetryTimeouts = true);

public sealed record ForgeTimeoutPolicy(
    TimeSpan CommandTimeout,
    TimeSpan? ConnectionTimeout = null);

public sealed class ForgeCircuitBreaker
{
    private int _failures;
    private DateTimeOffset? _openedUntil;

    public bool IsOpen => _openedUntil is not null && _openedUntil > DateTimeOffset.UtcNow;

    public void RecordSuccess()
    {
        _failures = 0;
        _openedUntil = null;
    }

    public void RecordFailure(int threshold = 5, TimeSpan? breakDuration = null)
    {
        _failures++;

        if (_failures >= threshold)
        {
            _openedUntil = DateTimeOffset.UtcNow.Add(breakDuration ?? TimeSpan.FromSeconds(30));
        }
    }
}

public static class ForgeReliabilityExecutor
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ForgeRetryPolicy? retryPolicy = null,
        ForgeCircuitBreaker? circuitBreaker = null,
        CancellationToken cancellationToken = default)
    {
        retryPolicy ??= new ForgeRetryPolicy();
        var delay = retryPolicy.InitialDelay ?? TimeSpan.FromMilliseconds(100);

        if (circuitBreaker?.IsOpen == true)
        {
            throw new InvalidOperationException("ForgeORM circuit breaker is open.");
        }

        Exception? last = null;

        for (var attempt = 0; attempt <= retryPolicy.MaxRetries; attempt++)
        {
            try
            {
                var result = await operation(cancellationToken);
                circuitBreaker?.RecordSuccess();
                return result;
            }
            catch (Exception ex) when (attempt < retryPolicy.MaxRetries && ShouldRetry(ex, retryPolicy))
            {
                last = ex;
                circuitBreaker?.RecordFailure();
                await Task.Delay(TimeSpan.FromMilliseconds(delay.TotalMilliseconds * Math.Pow(2, attempt)), cancellationToken);
            }
        }

        throw last ?? new InvalidOperationException("ForgeORM reliability execution failed.");
    }

    private static bool ShouldRetry(Exception ex, ForgeRetryPolicy policy)
    {
        var message = ex.Message;
        if (policy.RetryDeadlocks && message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)) return true;
        if (policy.RetryTimeouts && message.Contains("timeout", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
