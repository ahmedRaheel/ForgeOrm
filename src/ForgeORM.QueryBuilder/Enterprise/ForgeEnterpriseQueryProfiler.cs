using System.Diagnostics;

namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Profiles query execution blocks.
/// </summary>
public sealed class ForgeEnterpriseQueryProfiler
{
    /// <summary>
    /// Profiles an async query operation.
    /// </summary>
    public async ValueTask<(T Result, ForgeEnterpriseQueryProfile Profile)> ProfileAsync<T>(string name, ForgeSqlQuery query, Func<ValueTask<T>> execute, long rowsReturned = 0)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await execute().ConfigureAwait(false);
        stopwatch.Stop();

        return (result, new ForgeEnterpriseQueryProfile
        {
            Name = name,
            Sql = query.Sql,
            Duration = stopwatch.Elapsed,
            RowsReturned = rowsReturned,
            IndexSuggestions = ForgeEnterpriseIndexSuggestionEngine.Suggest(query).ToArray()
        });
    }
}
