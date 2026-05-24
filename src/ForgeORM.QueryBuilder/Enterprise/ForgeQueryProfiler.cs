using System.Diagnostics;

namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Profiles query execution blocks.
/// </summary>
public sealed class ForgeQueryProfiler
{
    /// <summary>
    /// Profiles an async query operation.
    /// </summary>
    public async ValueTask<(T Result, ForgeQueryProfile Profile)> ProfileAsync<T>(string name, ForgeSqlQuery query, Func<ValueTask<T>> execute, long rowsReturned = 0)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await execute().ConfigureAwait(false);
        stopwatch.Stop();

        return (result, new ForgeQueryProfile
        {
            Name = name,
            Sql = query.Sql,
            Duration = stopwatch.Elapsed,
            RowsReturned = rowsReturned,
            IndexSuggestions = ForgeIndexSuggestionEngine.Suggest(query).ToArray()
        });
    }
}
