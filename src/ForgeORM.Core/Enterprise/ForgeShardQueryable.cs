using System.Buffers;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

public sealed class ForgeShardQueryable<T> : IForgeQuery<T>
{
    internal IForgeQuery<T> Inner { get; }

    internal ForgeShardQueryable(IForgeQuery<T> inner) => Inner = inner;

    public ForgeQueryExecutionOptions ExecutionOptions => Inner.ExecutionOptions;
    public IForgeQuery<T> Where(Expression<Func<T, bool>> predicate) { Inner.Where(predicate); return this; }
    public IForgeQuery<T> Where(string condition, object? parameters = null) { Inner.Where(condition, parameters); return this; }
    public IForgeQuery<T> WhereSql(string condition, object? parameters = null) { Inner.WhereSql(condition, parameters); return this; }
    public IForgeQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate) { Inner.WhereIf(condition, predicate); return this; }
    public IForgeQuery<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null) { Inner.WhereSqlIf(condition, sqlCondition, parameters); return this; }
    public IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector) { Inner.OrderBy(keySelector); return this; }
    public IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) { Inner.OrderByDescending(keySelector); return this; }
    public IForgeQuery<T> OrderBy(string orderBy) { Inner.OrderBy(orderBy); return this; }
    public IForgeQuery<T> OrderBySql(string orderBy) { Inner.OrderBySql(orderBy); return this; }
    public IForgeQuery<T> Skip(int count) { Inner.Skip(count); return this; }
    public IForgeQuery<T> Take(int count) { Inner.Take(count); return this; }
    public IForgeQuery<T> TemporalAll() { Inner.TemporalAll(); return this; }
    public IForgeQuery<T> TemporalAsOf(DateTime asOfUtc) { Inner.TemporalAsOf(asOfUtc); return this; }
    public IForgeQuery<T> TemporalBetween(DateTime fromUtc, DateTime toUtc) { Inner.TemporalBetween(fromUtc, toUtc); return this; }
    public IForgeQuery<T> TemporalContainedIn(DateTime fromUtc, DateTime toUtc) { Inner.TemporalContainedIn(fromUtc, toUtc); return this; }
    public IForgeQuery<T> Include<TProperty>(Expression<Func<T, TProperty>> navigation) { Inner.Include(navigation); return this; }
    public bool Any() => Inner.Any();
    public ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default) => Inner.AnyAsync(cancellationToken);
    public IReadOnlyList<T> ToList() => Inner.ToList();
    public ValueTask<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default) => Inner.ToListAsync(cancellationToken);
    public IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default) => Inner.StreamAsync(cancellationToken);
    public ValueTask ProcessInBatchesAsync(int batchSize, Func<IReadOnlyList<T>, ValueTask> processor, CancellationToken cancellationToken = default) => Inner.ProcessInBatchesAsync(batchSize, processor, cancellationToken);
    public T? FirstOrDefault() => Inner.FirstOrDefault();
    public ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) => Inner.FirstOrDefaultAsync(cancellationToken);
    public int Count() => Inner.Count();
    public ValueTask<int> CountAsync(CancellationToken cancellationToken = default) => Inner.CountAsync(cancellationToken);
    public ValueTask<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Inner.SumAsync(selector, cancellationToken);
    public ValueTask<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Inner.AverageAsync(selector, cancellationToken);
    public ValueTask<decimal> MinAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Inner.MinAsync(selector, cancellationToken);
    public ValueTask<decimal> MaxAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default) => Inner.MaxAsync(selector, cancellationToken);
    public ValueTask<ForgePagedResult<T>> PageAsync(int page, int pageSize, CancellationToken cancellationToken = default) => Inner.PageAsync(page, pageSize, cancellationToken);
    public string ToSql() => Inner.ToSql();

    public IForgeQuery<T> UnionShards() => this;
}
