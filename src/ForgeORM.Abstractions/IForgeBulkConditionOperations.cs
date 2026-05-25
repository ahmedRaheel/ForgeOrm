using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

public interface IForgeBulkConditionOperations
{
    /// <summary>
    /// Gets multiple entities by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to match.</param>
    /// <returns>The entities whose key values match the supplied ids.</returns>
    IReadOnlyList<T> GetByIds<T, TKey>(IEnumerable<TKey> ids);

    /// <summary>
    /// Gets multiple entities by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to match.</param>
    /// <returns>The entities whose selected key values match the supplied ids.</returns>
    IReadOnlyList<T> GetByIds<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids);

    /// <summary>
    /// Gets multiple entities by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to match.</param>
    /// <returns>The entities whose SQL key column values match the supplied ids.</returns>
    IReadOnlyList<T> GetByIdsSql<T, TKey>(string keyColumn, IEnumerable<TKey> ids);

    /// <summary>
    /// Gets multiple entities asynchronously by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to match.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The entities whose key values match the supplied ids.</returns>
    ValueTask<IReadOnlyList<T>> GetByIdsAsync<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple entities asynchronously by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to match.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The entities whose selected key values match the supplied ids.</returns>
    ValueTask<IReadOnlyList<T>> GetByIdsAsync<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple entities asynchronously by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to match.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The entities whose SQL key column values match the supplied ids.</returns>
    ValueTask<IReadOnlyList<T>> GetByIdsSqlAsync<T, TKey>(string keyColumn, IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to delete.</param>
    /// <returns>The number of deleted rows.</returns>
    int DeleteByIds<T, TKey>(IEnumerable<TKey> ids);

    /// <summary>
    /// Deletes multiple entities by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <returns>The number of deleted rows.</returns>
    int DeleteByIds<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids);

    /// <summary>
    /// Deletes multiple entities by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <returns>The number of deleted rows.</returns>
    int DeleteByIdsSql<T, TKey>(string keyColumn, IEnumerable<TKey> ids);

    /// <summary>
    /// Deletes multiple entities asynchronously by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    ValueTask<int> DeleteByIdsAsync<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities asynchronously by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    ValueTask<int> DeleteByIdsAsync<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities asynchronously by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    ValueTask<int> DeleteByIdsSqlAsync<T, TKey>(string keyColumn, IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <returns>The number of updated rows.</returns>
    int UpdateByIds<T, TKey>(IEnumerable<TKey> ids, object values);

    /// <summary>
    /// Updates multiple entities by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <returns>The number of updated rows.</returns>
    int UpdateByIds<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, object values);

    /// <summary>
    /// Updates multiple entities by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <returns>The number of updated rows.</returns>
    int UpdateByIdsSql<T, TKey>(string keyColumn, IEnumerable<TKey> ids, object values);

    /// <summary>
    /// Updates multiple entities asynchronously by their configured key column.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    ValueTask<int> UpdateByIdsAsync<T, TKey>(IEnumerable<TKey> ids, object values, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities asynchronously by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    ValueTask<int> UpdateByIdsAsync<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, object values, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities asynchronously by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to update.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    ValueTask<int> UpdateByIdsSqlAsync<T, TKey>(string keyColumn, IEnumerable<TKey> ids, object values, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes entities that match an expression condition.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="predicate">The expression condition used to select rows for deletion.</param>
    /// <returns>The number of deleted rows.</returns>
    int DeleteByCondition<T>(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Deletes entities that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <returns>The number of deleted rows.</returns>
    int DeleteByConditionSql<T>(string sqlCondition, object? parameters = null);

    /// <summary>
    /// Deletes entities asynchronously that match an expression condition.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="predicate">The expression condition used to select rows for deletion.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    ValueTask<int> DeleteByConditionAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes entities asynchronously that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    ValueTask<int> DeleteByConditionSqlAsync<T>(string sqlCondition, object? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates entities that match an expression condition.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="predicate">The expression condition used to select rows for update.</param>
    /// <returns>The number of updated rows.</returns>
    int UpdateByCondition<T>(object values, Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Updates entities that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <returns>The number of updated rows.</returns>
    int UpdateByConditionSql<T>(object values, string sqlCondition, object? parameters = null);

    /// <summary>
    /// Updates entities asynchronously that match an expression condition.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="predicate">The expression condition used to select rows for update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    ValueTask<int> UpdateByConditionAsync<T>(object values, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates entities asynchronously that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    ValueTask<int> UpdateByConditionSqlAsync<T>(object values, string sqlCondition, object? parameters = null, CancellationToken cancellationToken = default);
}
