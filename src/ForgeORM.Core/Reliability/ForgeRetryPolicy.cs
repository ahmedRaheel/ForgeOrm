namespace ForgeORM.Core.Reliability;

public sealed record ForgeRetryPolicy(
    int MaxRetries = 3,
    TimeSpan? InitialDelay = null,
    bool RetryDeadlocks = true,
    bool RetryTimeouts = true);
