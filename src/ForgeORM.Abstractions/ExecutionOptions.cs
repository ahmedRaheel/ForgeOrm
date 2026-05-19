namespace ForgeORM.Abstractions;

/// <summary>
/// Central execution options shared by raw SQL, expression queries, reports, frames, and future analytics queries.
/// </summary>
public sealed class ForgeQueryExecutionOptions
{
    public ForgeReadConsistency ReadConsistency { get; set; } = ForgeReadConsistency.Default;
    public ForgeLockBehavior LockBehavior { get; set; } = ForgeLockBehavior.None;
    public int? TimeoutSeconds { get; set; }
    public bool EnableStreaming { get; set; }
    public bool EnableProfiling { get; set; }
    public bool EnableCaching { get; set; }
    public bool EnableMonitoring { get; set; }
    public bool UseReadReplica { get; set; }
    public int? MaxConcurrency { get; set; }
    public string? QueryTag { get; set; }
}

public enum ForgeLockBehavior
{
    None,
    NoLock,
    ReadPast,
    UpdateLock,
    RowLock,
    HoldLock
}

public enum ForgeReadConsistency
{
    Default,
    ReadCommitted,
    ReadUncommitted,
    Snapshot,
    Serializable
}

/// <summary>
/// Contract for every executable ForgeORM query shape so common features can be applied centrally.
/// </summary>
public interface IForgeExecutableQuery
{
    ForgeQueryExecutionOptions ExecutionOptions { get; }
    string ToSql();
}
