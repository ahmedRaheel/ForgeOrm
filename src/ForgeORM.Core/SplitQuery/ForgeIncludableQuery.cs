using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Lightweight wrapper returned from Include so ThenInclude can infer the previous navigation type.
/// It delegates every operation to the underlying query; it does not add another execution pipeline.
/// </summary>
internal sealed class ForgeIncludableQuery<T, TProperty> : IForgeIncludableQuery<T, TProperty>
{
    public ForgeIncludableQuery(IForgeQuery<T> query) => Query = query ?? throw new ArgumentNullException(nameof(query));
    
    public IForgeQuery<T> Query { get; }
    public ForgeQueryExecutionOptions ExecutionOptions => Query.ExecutionOptions;
    public string ToSql() => Query.ToSql();
    public IForgeQuery<T> Where(Expression<Func<T, bool>> predicate) => Query.Where(predicate);
    public IForgeQuery<T> Where(string condition, object? parameters = null) => Query.Where(condition, parameters);
    public IForgeQuery<T> WhereSql(string condition, object? parameters = null) => Query.WhereSql(condition, parameters);
    public IForgeQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate) => Query.WhereIf(condition, predicate);
    public IForgeQuery<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null) => Query.WhereSqlIf(condition, sqlCondition, parameters);
    public IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector) => Query.OrderBy(keySelector);
    public IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) => Query.OrderByDescending(keySelector);
    public IForgeQuery<T> OrderBy(string orderBy) => Query.OrderBy(orderBy);
    public IForgeQuery<T> OrderBySql(string orderBy) => Query.OrderBySql(orderBy);
    public IForgeQuery<T> Skip(int count) => Query.Skip(count);
    public IForgeQuery<T> Take(int count) => Query.Take(count);
    public IForgeQuery<T> TemporalAll() => Query.TemporalAll();
    public IForgeQuery<T> TemporalAsOf(DateTime asOfUtc) => Query.TemporalAsOf(asOfUtc);
    public IForgeQuery<T> TemporalBetween(DateTime fromUtc, DateTime toUtc) => Query.TemporalBetween(fromUtc, toUtc);
    public IForgeQuery<T> TemporalContainedIn(DateTime fromUtc, DateTime toUtc) => Query.TemporalContainedIn(fromUtc, toUtc);
    public IForgeQuery<T> Include<TNextProperty>(Expression<Func<T, TNextProperty>> navigation) => Query.Include(navigation);
    public bool Any() => Query.Any();
    public ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default) => Query.AnyAsync(cancellationToken);
    public IReadOnlyList<T> ToList() => Query.ToList();
    public ValueTask<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default) => Query.ToListAsync(cancellationToken);
    public IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default) => Query.StreamAsync(cancellationToken);
    public ValueTask ProcessInBatchesAsync(int batchSize, Func<IReadOnlyList<T>, ValueTask> processor, CancellationToken cancellationToken = default) => Query.ProcessInBatchesAsync(batchSize, processor, cancellationToken);
    public T? FirstOrDefault() => Query.FirstOrDefault();
    public ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) => Query.FirstOrDefaultAsync(cancellationToken);
    public int Count() => Query.Count();
    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default) => Query.CountAsync(cancellationToken);
    public ValueTask<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Query.SumAsync(selector, cancellationToken);
    public ValueTask<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Query.AverageAsync(selector, cancellationToken);
    public ValueTask<decimal> MinAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Query.MinAsync(selector, cancellationToken);
    public ValueTask<decimal> MaxAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Query.MaxAsync(selector, cancellationToken);
    public ValueTask<ForgePagedResult<T>> PageAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Query.PageAsync(page, pageSize, cancellationToken);
}
