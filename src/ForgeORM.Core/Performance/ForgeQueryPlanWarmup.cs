using System.Data;
using System.Data.Common;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Warms ForgeORM query-plan and parameter-binder caches during application startup.
/// This removes first-request reflection/compilation cost from latency-sensitive endpoints.
/// </summary>
public static class ForgeQueryPlanWarmup
{
    /// <summary>
    /// Warms a query plan without executing SQL.
    /// </summary>
    public static void WarmupQuery<T>(DbConnection connection, string sql, object? parameters = null, CommandType commandType = CommandType.Text)
        => _ = ForgeCompiledExecutionPlanCache.GetOrAdd<T>(connection, sql, parameters, commandType, CommandBehavior.SequentialAccess);

    /// <summary>
    /// Warms multiple query plans without executing SQL.
    /// </summary>
    public static void WarmupQueries<T>(DbConnection connection, IEnumerable<(string Sql, object? Parameters)> queries, CommandType commandType = CommandType.Text)
    {
        foreach (var query in queries)
            WarmupQuery<T>(connection, query.Sql, query.Parameters, commandType);
    }
}
