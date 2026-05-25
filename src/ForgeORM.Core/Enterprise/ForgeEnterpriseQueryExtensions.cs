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
