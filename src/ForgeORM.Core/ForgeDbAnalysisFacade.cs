using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Facade for database-level diagnostics and SQL analysis helpers.
/// </summary>
public sealed class ForgeDbAnalysisFacade
{
    private readonly ForgeDb _db;

    internal ForgeDbAnalysisFacade(ForgeDb db)
    {
        _db = db;
    }

    /// <summary>
    /// Analyzes raw SQL using the configured ForgeORM analyzer.
    /// </summary>
    public ForgeORM.Abstractions.ForgeQueryAnalysis Analyze(string sql)
    {
        return _db.Analyze(sql);
    }

    /// <summary>
    /// Analyzes a rendered query builder SQL statement and returns QueryBuilder-specific advice.
    /// </summary>
    public ForgeQueryBuilderAnalysis Analyze<TEntity>(ForgeQueryBuilder<TEntity> query)
        where TEntity : class, new()
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        return query.Analyze();
    }

    /// <summary>
    /// Returns captured query-builder profile entries.
    /// </summary>
    public IReadOnlyList<ForgeQueryBuilderProfileEntry> Profiles()
    {
        return ForgeQueryBuilderProfiler.Snapshot();
    }

    /// <summary>
    /// Clears captured query-builder profile entries.
    /// </summary>
    public void ClearProfiles()
    {
        ForgeQueryBuilderProfiler.Clear();
    }
}
