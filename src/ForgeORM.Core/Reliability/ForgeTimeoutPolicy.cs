namespace ForgeORM.Core.Reliability;

public sealed record ForgeTimeoutPolicy(
    TimeSpan CommandTimeout,
    TimeSpan? ConnectionTimeout = null);
