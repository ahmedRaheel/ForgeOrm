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

    /// <summary>Analyzes raw SQL using the configured ForgeORM analyzer.</summary>
    public ForgeORM.Abstractions. ForgeQueryAnalysis Analyze(string sql) => _db.Analyze(sql);

    /// <summary>Analyzes a rendered query builder SQL statement.</summary>
    public ForgeQueryBuilderAnalysis Analyze<TEntity>(ForgeQueryBuilder<TEntity> query)
        where TEntity : class, new()
    {
        return query.Analyze();
    }
}

public partial class ForgeDb
{
    private ForgeDbAnalysisFacade? _analysis;

    /// <summary>Database diagnostics and SQL analysis facade.</summary>
    public ForgeDbAnalysisFacade Analysis => _analysis ??= new ForgeDbAnalysisFacade(this);
}
