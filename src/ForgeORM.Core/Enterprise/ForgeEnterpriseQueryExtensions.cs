using System.Linq.Expressions;

namespace ForgeORM.Core;

/// <summary>
/// Enterprise query helpers for complex SQL patterns.
/// </summary>
public static class ForgeEnterpriseQueryExtensions
{
    public static ForgeQueryBuilder<TEntity> WithRecursiveCte<TEntity>(
        this ForgeQueryBuilder<TEntity> query,
        string cteName,
        string anchorSql,
        string recursiveSql)
        where TEntity : class, new()
    {
        var original = query.ToSql();
        query.From($"(WITH {cteName} AS ({anchorSql} UNION ALL {recursiveSql}) {original}) {cteName}_q");
        return query;
    }

    public static ForgeQueryBuilder<TEntity> WhereInSubQuery<TEntity>(
        this ForgeQueryBuilder<TEntity> query,
        string column,
        string subQuerySql,
        object? parameters = null)
        where TEntity : class, new()
        => query.WhereSql($"{column} IN ({subQuerySql})", parameters);

    public static ForgeQueryBuilder<TEntity> WhereNotInSubQuery<TEntity>(
        this ForgeQueryBuilder<TEntity> query,
        string column,
        string subQuerySql,
        object? parameters = null)
        where TEntity : class, new()
        => query.WhereSql($"{column} NOT IN ({subQuerySql})", parameters);

    public static ForgeQueryBuilder<TEntity> TemporalBetween<TEntity>(
        this ForgeQueryBuilder<TEntity> query,
        DateTimeOffset from,
        DateTimeOffset to)
        where TEntity : class, new()
    {
        var table = query.TableName.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        query.From($"{table} FOR SYSTEM_TIME BETWEEN @TemporalFrom AND @TemporalTo");
        return query.WhereSql("1 = 1", new { TemporalFrom = from, TemporalTo = to });
    }

    public static ForgeQueryBuilder<TEntity> Lateral<TEntity>(
        this ForgeQueryBuilder<TEntity> query,
        string subQuerySql,
        string alias)
        where TEntity : class, new()
        => query.CrossApply(subQuerySql, alias);
}

/// <summary>
/// Fluent compiled query registration/execution foundation.
/// </summary>
public sealed class ForgeCompiledQuery<TEntity>
    where TEntity : class, new()
{
    private readonly ForgeDb _db;
    private readonly string _name;
    private readonly ForgeQueryBuilder<TEntity> _builder;

    internal ForgeCompiledQuery(ForgeDb db, string name)
    {
        _db = db;
        _name = name;
        _builder = db.Query<TEntity>().Tag(name);
    }

    public ForgeCompiledQuery<TEntity> Configure(Action<ForgeQueryBuilder<TEntity>> configure)
    {
        configure(_builder);
        return this;
    }

    public Task<IReadOnlyList<TEntity>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return _builder.Profile(_name).CacheFor(TimeSpan.FromMinutes(10)).ToListAsync(cancellationToken);
    }

    public string ToSql() => _builder.ToSql();
}

public static class ForgeCompiledQueryExtensions
{
    public static ForgeCompiledQuery<TEntity> CompiledQuery<TEntity>(this ForgeDb db, string name)
        where TEntity : class, new()
        => new(db, name);
}
