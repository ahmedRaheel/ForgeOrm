using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

public interface IForgeQuery<T> : IForgeExecutableQuery
/// <summary>
/// Defines the Where operation.
/// </summary>
/// <param name="predicate">The predicate value.</param>
/// <returns>The result of the Where operation.</returns>
{
    /// <summary>
    /// Defines the Where operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the Where operation.</returns>
    IForgeQuery<T> Where(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Defines the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Where operation.</returns>
    IForgeQuery<T> Where(string condition, object? parameters = null);
    /// <summary>
    /// Defines the WhereSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    IForgeQuery<T> WhereSql(string condition, object? parameters = null);
    /// <summary>
    /// Defines the WhereIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the WhereIf operation.</returns>
    IForgeQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Defines the WhereSqlIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="sqlCondition">The sqlCondition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSqlIf operation.</returns>
    IForgeQuery<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null);
    /// <summary>
    /// Defines the OrderBy operation.
    /// </summary>
    /// <param name="keySelector">The keySelector value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector);
    /// <summary>
    /// Defines the OrderByDescending operation.
    /// </summary>
    /// <param name="keySelector">The keySelector value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
    IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector);
    /// <summary>
    /// Defines the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    IForgeQuery<T> OrderBy(string orderBy);
    /// <summary>
    /// Defines the OrderBySql operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBySql operation.</returns>
    IForgeQuery<T> OrderBySql(string orderBy);
    /// <summary>
    /// Defines the Skip operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the Skip operation.</returns>
    IForgeQuery<T> Skip(int count);
    /// <summary>
    /// Defines the Take operation.
    /// </summary>
    /// <param name="count">The count value.</param>
    /// <returns>The result of the Take operation.</returns>
    IForgeQuery<T> Take(int count);

    IForgeQuery<T> TemporalAll();
    IForgeQuery<T> TemporalAsOf(DateTime asOfUtc);
    IForgeQuery<T> TemporalBetween(DateTime fromUtc, DateTime toUtc);
    IForgeQuery<T> TemporalContainedIn(DateTime fromUtc, DateTime toUtc);

    /// <summary>
    /// Includes a reference or collection navigation property. Included navigations are loaded by split query only.
    /// </summary>
    /// <typeparam name="TProperty">The navigation property type.</typeparam>
    /// <param name="navigation">Navigation selector, for example x => x.Items or x => x.Customer.</param>
    /// <returns>The current query.</returns>
    IForgeQuery<T> Include<TProperty>(Expression<Func<T, TProperty>> navigation);

    /// <summary>
    /// Defines the Any operation.
    /// </summary>
    /// <returns>The result of the Any operation.</returns>
    bool Any();
    /// <summary>
    /// Defines the AnyAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the AnyAsync operation.</returns>
    ValueTask<bool> AnyAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ToList operation.
    /// </summary>
    /// <returns>The result of the ToList operation.</returns>
    IReadOnlyList<T> ToList();
    /// <summary>
    /// Defines the ToListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    ValueTask<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>Streams rows with DbDataReader sequential access and MSIL materialization.</summary>
    IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default);

    /// <summary>Processes rows in fixed-size batches without requiring callers to load the whole result set.</summary>
    ValueTask ProcessInBatchesAsync(int batchSize, Func<IReadOnlyList<T>, ValueTask> processor, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the FirstOrDefault operation.
    /// </summary>
    /// <returns>The result of the FirstOrDefault operation.</returns>
    T? FirstOrDefault();
    /// <summary>
    /// Defines the FirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FirstOrDefaultAsync operation.</returns>
    ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the Count operation.
    /// </summary>
    /// <returns>The result of the Count operation.</returns>
    int Count();
    /// <summary>
    /// Defines the CountAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CountAsync operation.</returns>
    ValueTask<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes SUM for the selected numeric column.
    /// </summary>
    /// <param name="selector">Column selector used for SUM.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate result.</returns>
    ValueTask<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes AVG for the selected numeric column.
    /// </summary>
    /// <param name="selector">Column selector used for AVG.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate result.</returns>
    ValueTask<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes MIN for the selected numeric column.
    /// </summary>
    /// <param name="selector">Column selector used for MIN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate result.</returns>
    ValueTask<decimal> MinAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes MAX for the selected numeric column.
    /// </summary>
    /// <param name="selector">Column selector used for MAX.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate result.</returns>
    ValueTask<decimal> MaxAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes expression-based paging using the current query filters and ordering.
    /// When no ordering exists, SQL Server rendering falls back to ORDER BY 1.
    /// </summary>
    /// <param name="page">One-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The paged result.</returns>
    ValueTask<ForgePagedResult<T>> PageAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
