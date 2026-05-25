namespace ForgeORM.Core.Reliability;

public static class ForgeReliabilityExecutor
{
    public static async ValueTask<T> ExecuteAsync<T>(
        Func<CancellationToken, ValueTask<T>> operation,
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
