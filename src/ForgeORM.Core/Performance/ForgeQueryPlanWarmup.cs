using System.Data;
using System.Data.Common;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Warms ForgeORM query-plan, direct-execution, materializer and parameter-binder caches during application startup.
/// This removes first-request reflection/compilation cost from latency-sensitive endpoints.
/// </summary>
public static class ForgeQueryPlanWarmup
{
    /// <summary>
    /// Warms a query plan without executing SQL. The framework-level direct executor is warmed first;
    /// unsupported shapes fall back to the compiled pipeline cache.
    /// </summary>
    public static void WarmupQuery<T>(DbConnection connection, string sql, object? parameters = null, CommandType commandType = CommandType.Text)
    {
        if (ForgeDirectQueryExecutor.Precompile(sql, parameters, commandType))
            return;

        _ = ForgeCompiledExecutionPlanCache.GetOrAdd<T>(connection, sql, parameters, commandType, CommandBehavior.SequentialAccess);
    }

    /// <summary>
    /// Warms a single-row query plan without executing SQL.
    /// </summary>
    public static void WarmupSingle<T>(DbConnection connection, string sql, object? parameters = null, CommandType commandType = CommandType.Text)
    {
        if (ForgeDirectQueryExecutor.Precompile(sql, parameters, commandType))
            return;

        _ = ForgeCompiledExecutionPlanCache.GetOrAdd<T>(connection, sql, parameters, commandType, CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
    }

    /// <summary>
    /// Warms multiple query plans without executing SQL.
    /// </summary>
    public static void WarmupQueries<T>(DbConnection connection, IEnumerable<(string Sql, object? Parameters)> queries, CommandType commandType = CommandType.Text)
    {
        foreach (var query in queries)
            WarmupQuery<T>(connection, query.Sql, query.Parameters, commandType);
    }

    /// <summary>
    /// Warms multiple single-row query plans without executing SQL.
    /// </summary>
    public static void WarmupSingles<T>(DbConnection connection, IEnumerable<(string Sql, object? Parameters)> queries, CommandType commandType = CommandType.Text)
    {
        foreach (var query in queries)
            WarmupSingle<T>(connection, query.Sql, query.Parameters, commandType);
    }
}
