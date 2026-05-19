using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Shared enterprise query options. Works with expression queries now and is reusable by reports/DataFrames/AST queries later.
/// </summary>
public static class ForgeExecutionExtensions
{
    public static T NoLock<T>(this T query) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.LockBehavior = ForgeLockBehavior.NoLock;
        query.ExecutionOptions.ReadConsistency = ForgeReadConsistency.ReadUncommitted;
        return query;
    }

    public static T ReadPast<T>(this T query) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.LockBehavior = ForgeLockBehavior.ReadPast;
        return query;
    }

    public static T UpdateLock<T>(this T query) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.LockBehavior = ForgeLockBehavior.UpdateLock;
        return query;
    }

    public static T RowLock<T>(this T query) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.LockBehavior = ForgeLockBehavior.RowLock;
        return query;
    }

    public static T HoldLock<T>(this T query) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.LockBehavior = ForgeLockBehavior.HoldLock;
        return query;
    }

    public static T SnapshotRead<T>(this T query) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.ReadConsistency = ForgeReadConsistency.Snapshot;
        return query;
    }

    public static T WithReadConsistency<T>(this T query, ForgeReadConsistency consistency) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.ReadConsistency = consistency;
        return query;
    }

    public static T UseReadReplica<T>(this T query) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.UseReadReplica = true;
        return query;
    }

    public static T Timeout<T>(this T query, int seconds) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.TimeoutSeconds = seconds;
        return query;
    }

    public static T QueryTag<T>(this T query, string tag) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.QueryTag = tag;
        return query;
    }

    public static T EnableProfiling<T>(this T query) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.EnableProfiling = true;
        return query;
    }

    public static T EnableMonitoring<T>(this T query) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.EnableMonitoring = true;
        return query;
    }

    public static T MaxConcurrency<T>(this T query, int maxConcurrency) where T : IForgeExecutableQuery
    {
        query.ExecutionOptions.MaxConcurrency = maxConcurrency;
        return query;
    }
}
