using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

public interface IForgeDb :
    IForgeRawSql,
    IForgeStoredProcedures,
    IForgeDatabaseFunctions,
    IForgeRepository,
    IForgeQueryableFactory,
    IForgeSplitQueryFactory,
    IForgeBulkOperations,
    IForgeBulkConditionOperations,
    IForgeTransactionManager,
    IForgeDiagnostics
{
    IForgeDatabaseProvider Provider { get; }
}

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
    Task<IReadOnlyList<T>> GetByIdsAsync<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple entities asynchronously by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to match.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The entities whose selected key values match the supplied ids.</returns>
    Task<IReadOnlyList<T>> GetByIdsAsync<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple entities asynchronously by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to read.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to match.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The entities whose SQL key column values match the supplied ids.</returns>
    Task<IReadOnlyList<T>> GetByIdsSqlAsync<T, TKey>(string keyColumn, IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

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
    Task<int> DeleteByIdsAsync<T, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities asynchronously by a key column selected with an expression.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    Task<int> DeleteByIdsAsync<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities asynchronously by an explicit SQL key column.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL column name used in the IN condition.</param>
    /// <param name="ids">The key values to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    Task<int> DeleteByIdsSqlAsync<T, TKey>(string keyColumn, IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

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
    Task<int> UpdateByIdsAsync<T, TKey>(IEnumerable<TKey> ids, object values, CancellationToken cancellationToken = default);

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
    Task<int> UpdateByIdsAsync<T, TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids, object values, CancellationToken cancellationToken = default);

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
    Task<int> UpdateByIdsSqlAsync<T, TKey>(string keyColumn, IEnumerable<TKey> ids, object values, CancellationToken cancellationToken = default);

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
    Task<int> DeleteByConditionAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes entities asynchronously that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of deleted rows.</returns>
    Task<int> DeleteByConditionSqlAsync<T>(string sqlCondition, object? parameters = null, CancellationToken cancellationToken = default);

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
    Task<int> UpdateByConditionAsync<T>(object values, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates entities asynchronously that match a SQL condition.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <param name="sqlCondition">The SQL condition used after the WHERE keyword.</param>
    /// <param name="parameters">The SQL condition parameters.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of updated rows.</returns>
    Task<int> UpdateByConditionSqlAsync<T>(object values, string sqlCondition, object? parameters = null, CancellationToken cancellationToken = default);
}

public interface IForgeRawSql
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="sql">The sql value.</param>
/// <param name="parameters">The parameters value.</param>
/// <param name="timeoutSeconds">The timeoutSeconds value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    T QueryFirst<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T> QueryFirstAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    T? QueryFirstOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    T QuerySingle<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T> QuerySingleAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    T? QuerySingleOrDefault<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the Execute operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Execute operation.</returns>
    int Execute(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the ExecuteAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the QueryMultiple operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the QueryMultiple operation.</returns>
    IForgeGridReader QueryMultiple(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the QueryMultipleAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the QueryMultipleAsync operation.</returns>
    Task<IForgeGridReader> QueryMultipleAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
}

public interface IForgeStoredProcedures
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="procedureName">The procedureName value.</param>
/// <param name="parameters">The parameters value.</param>
/// <param name="timeoutSeconds">The timeoutSeconds value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    IEnumerable<T> QueryProcedure<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<IReadOnlyList<T>> QueryProcedureAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    T? QueryProcedureSingleOrDefault<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T?> QueryProcedureSingleOrDefaultAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ExecuteProcedure operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the ExecuteProcedure operation.</returns>
    int ExecuteProcedure(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the ExecuteProcedureAsync operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteProcedureAsync operation.</returns>
    Task<int> ExecuteProcedureAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    T? ExecuteProcedureScalar<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T?> ExecuteProcedureScalarAsync<T>(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the QueryProcedureMultiple operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the QueryProcedureMultiple operation.</returns>
    IForgeGridReader QueryProcedureMultiple(string procedureName, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the QueryProcedureMultipleAsync operation.
    /// </summary>
    /// <param name="procedureName">The procedureName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the QueryProcedureMultipleAsync operation.</returns>
    Task<IForgeGridReader> QueryProcedureMultipleAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
}

public interface IForgeDatabaseFunctions
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="functionName">The functionName value.</param>
/// <param name="parameters">The parameters value.</param>
/// <param name="timeoutSeconds">The timeoutSeconds value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    T? ExecuteFunction<T>(string functionName, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T?> ExecuteFunctionAsync<T>(string functionName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="functionSql">The functionSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    IReadOnlyList<T> QueryFunction<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="functionSql">The functionSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<IReadOnlyList<T>> QueryFunctionAsync<T>(string functionSql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
}

public interface IForgeRepository
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="id">The id value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="id">The id value.</param>
    /// <returns>The result of the T operation.</returns>
    T? GetById<T>(object id);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="code">The code value.</param>
    /// <returns>The result of the T operation.</returns>
    T? GetByCode<T>(string code);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="code">The code value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T?> GetByCodeAsync<T>(string code, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="ids">The ids value.</param>
    /// <returns>The result of the T operation.</returns>
    IReadOnlyList<T> GetByIds<T>(IReadOnlyCollection<int> ids);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="ids">The ids value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<IReadOnlyList<T>> GetByIdsAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the T operation.</returns>
    int Insert<T>(T entity);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<int> InsertAsync<T>(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple independent entities in a single transactional operation.
    /// Use this when the rows do not form a parent-child aggregate graph.
    /// </summary>
    /// <typeparam name="T">The entity type to insert.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of inserted rows.</returns>
    Task<int> InsertManyAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts an aggregate root and every public child collection automatically in one transaction.
    /// The parent is inserted first, the generated parent key is copied into matching child foreign keys,
    /// then child rows are inserted recursively.
    /// </summary>
    /// <typeparam name="T">The aggregate root entity type.</typeparam>
    /// <param name="entity">The parent entity containing child collection properties.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The same entity instance after key propagation.</returns>
    Task<T> InsertGraphAsync<T>(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an aggregate root and synchronizes its child collections in one transaction.
    /// Existing children are updated, new children are inserted, and missing children can optionally be deleted.
    /// </summary>
    /// <typeparam name="T">The aggregate root entity type.</typeparam>
    /// <param name="entity">The parent entity containing the desired graph state.</param>
    /// <param name="deleteMissingChildren">When true, child rows not present in the supplied graph are deleted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> UpdateGraphAsync<T>(T entity, bool deleteMissingChildren = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a parent row and selected child collections into a single aggregate object.
    /// Include names must match public collection property names such as Items, Payments, or Notes.
    /// </summary>
    /// <typeparam name="T">The aggregate root entity type.</typeparam>
    /// <param name="id">The parent primary-key value.</param>
    /// <param name="includes">Collection property names to load.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The parent with requested child collections, or null when the parent is not found.</returns>
    Task<T?> GetGraphAsync<T>(object id, IEnumerable<string>? includes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a parent row and its child rows in one transaction.
    /// Child tables are deleted first to satisfy foreign-key constraints, then the parent is deleted.
    /// </summary>
    /// <typeparam name="T">The aggregate root entity type.</typeparam>
    /// <param name="id">The parent primary-key value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> DeleteGraphAsync<T>(object id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <returns>The result of the T operation.</returns>
    int Update<T>(T entity);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<int> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="id">The id value.</param>
    /// <returns>The result of the T operation.</returns>
    int Delete<T>(object id);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<int> DeleteAsync<T>(object id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the T operation.</returns>
    ForgePagedResult<T> Page<T>(ForgePageRequest request);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<ForgePagedResult<T>> PageAsync<T>(ForgePageRequest request, CancellationToken cancellationToken = default);
}

public interface IForgeQueryableFactory
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    IForgeQuery<T> Set<T>();
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    IForgeQuery<T> From<T>();
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the T operation.</returns>
    IForgeQuery<T> Sql<T>(string sql, object? parameters = null);
}

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
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
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
    Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>Streams rows with DbDataReader sequential access and MSIL materialization.</summary>
    IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default);

    /// <summary>Processes rows in fixed-size batches without requiring callers to load the whole result set.</summary>
    Task ProcessInBatchesAsync(int batchSize, Func<IReadOnlyList<T>, Task> processor, CancellationToken cancellationToken = default);
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
    Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
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
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes SUM for the selected numeric column.
    /// </summary>
    /// <param name="selector">Column selector used for SUM.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate result.</returns>
    Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes AVG for the selected numeric column.
    /// </summary>
    /// <param name="selector">Column selector used for AVG.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate result.</returns>
    Task<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes MIN for the selected numeric column.
    /// </summary>
    /// <param name="selector">Column selector used for MIN.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate result.</returns>
    Task<decimal> MinAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes MAX for the selected numeric column.
    /// </summary>
    /// <param name="selector">Column selector used for MAX.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The aggregate result.</returns>
    Task<decimal> MaxAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes expression-based paging using the current query filters and ordering.
    /// When no ordering exists, SQL Server rendering falls back to ORDER BY 1.
    /// </summary>
    /// <param name="page">One-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The paged result.</returns>
    Task<ForgePagedResult<T>> PageAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

public interface IForgeSplitQueryFactory
/// <summary>
/// Defines the TParent operation.
/// </summary>
/// <typeparam name="TParent">The type used by the operation.</typeparam>
/// <returns>The result of the TParent operation.</returns>
{
    /// <summary>
    /// Defines the TParent operation.
    /// </summary>
    /// <typeparam name="TParent">The type used by the operation.</typeparam>
    /// <returns>The result of the TParent operation.</returns>
    IForgeSplitQuery<TParent> Split<TParent>();
}

public interface IForgeSplitQuery<TParent>
{
    IForgeSplitQuery<TParent> IncludeMany<TChild, TKey>(
        Func<IReadOnlyCollection<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
        /// <summary>
        /// Defines the TChild operation.
        /// </summary>
        /// <typeparam name="TChild">The type used by the operation.</typeparam>
        /// <param name="childTable">The childTable value.</param>
        /// <param name="parentKey">The parentKey value.</param>
        /// <param name="childForeignKey">The childForeignKey value.</param>
        /// <param name="target">The target value.</param>
        /// <param name="backingField">The backingField value.</param>
        /// <param name="childWhereSql">The childWhereSql value.</param>
        /// <returns>The result of the TChild operation.</returns>
        where TKey : notnull;

/// <summary>

/// Defines the TChild operation.

/// </summary>

/// <typeparam name="TChild">The type used by the operation.</typeparam>

/// <param name="childTable">The childTable value.</param>

/// <param name="parentKey">The parentKey value.</param>

/// <param name="childForeignKey">The childForeignKey value.</param>

/// <param name="target">The target value.</param>

/// <param name="backingField">The backingField value.</param>

/// <param name="childWhereSql">The childWhereSql value.</param>

/// <returns>The result of the TChild operation.</returns>

    /// <summary>
    /// Defines the TChild operation.
    /// </summary>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <param name="childTable">The childTable value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="target">The target value.</param>
    /// <param name="backingField">The backingField value.</param>
    /// <param name="childWhereSql">The childWhereSql value.</param>
    /// <returns>The result of the TChild operation.</returns>
    IForgeSplitQuery<TParent> IncludeMany<TChild>(
        /// <summary>
        /// Defines the Any operation.
        /// </summary>
        /// <param name="parentSql">The parentSql value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <returns>The result of the Any operation.</returns>
        string childTable,
        /// <summary>
        /// Defines the Any operation.
        /// </summary>
        /// <param name="parentSql">The parentSql value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <returns>The result of the Any operation.</returns>
        string parentKey = "Id",
        /// <summary>
        /// Defines the Any operation.
        /// </summary>
        /// <param name="parentSql">The parentSql value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <returns>The result of the Any operation.</returns>
        string childForeignKey = "ParentId",
        /// <summary>
        /// Defines the Any operation.
        /// </summary>
        /// <param name="parentSql">The parentSql value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <returns>The result of the Any operation.</returns>
        Expression<Func<TParent, IEnumerable<TChild>>>? target = null,
        /// <summary>
        /// Defines the Any operation.
        /// </summary>
        /// <param name="parentSql">The parentSql value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <returns>The result of the Any operation.</returns>
        string? backingField = null,
        /// <summary>
        /// Defines the Any operation.
        /// </summary>
        /// <param name="parentSql">The parentSql value.</param>
        /// <param name="parameters">The parameters value.</param>
        /// <returns>The result of the Any operation.</returns>
        string? childWhereSql = null);

/// <summary>

/// Defines the Any operation.

/// </summary>

/// <param name="parentSql">The parentSql value.</param>

/// <param name="parameters">The parameters value.</param>

/// <returns>The result of the Any operation.</returns>

    /// <summary>
    /// Defines the Any operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Any operation.</returns>
    bool Any(string parentSql, object? parameters = null);
    /// <summary>
    /// Defines the AnyAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the AnyAsync operation.</returns>
    Task<bool> AnyAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the FirstOrDefault operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the FirstOrDefault operation.</returns>
    TParent? FirstOrDefault(string parentSql, object? parameters = null);
    /// <summary>
    /// Defines the FirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FirstOrDefaultAsync operation.</returns>
    Task<TParent?> FirstOrDefaultAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ToList operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the ToList operation.</returns>
    IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null);
    /// <summary>
    /// Defines the ToListAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    Task<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default);
}

public interface IForgeBulkOperations
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="rows">The rows value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    void BulkInsert<T>(IReadOnlyCollection<T> rows);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    void BulkInsert<T>(string tableName, IReadOnlyCollection<T> rows);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task BulkInsertAsync<T>(IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task BulkInsertAsync<T>(string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <returns>The result of the T operation.</returns>
    void BulkUpdate<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id");
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <returns>The result of the T operation.</returns>
    void BulkUpdate<T>(string tableName, IReadOnlyCollection<T> rows, string keyColumn = "Id");
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task BulkUpdateAsync<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task BulkUpdateAsync<T>(string tableName, IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="ids">The ids value.</param>
    /// <returns>The result of the T operation.</returns>
    void BulkDelete<T>(IReadOnlyCollection<int> ids);
    /// <summary>
    /// Defines the BulkDelete operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="ids">The ids value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    void BulkDelete(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id");
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="ids">The ids value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task BulkDeleteAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the BulkDeleteAsync operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="ids">The ids value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the BulkDeleteAsync operation.</returns>
    Task BulkDeleteAsync(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id", CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <returns>The result of the T operation.</returns>
    void BulkMerge<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id");
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task BulkMergeAsync<T>(IReadOnlyCollection<T> rows, string keyColumn = "Id", CancellationToken cancellationToken = default);
}

public interface IForgeTransactionManager
/// <summary>
/// Defines the BeginTransaction operation.
/// </summary>
/// <returns>The result of the BeginTransaction operation.</returns>
{
    /// <summary>
    /// Defines the BeginTransaction operation.
    /// </summary>
    /// <returns>The result of the BeginTransaction operation.</returns>
    IForgeTransaction BeginTransaction();
    /// <summary>
    /// Defines the BeginTransactionAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the BeginTransactionAsync operation.</returns>
    Task<IForgeTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IForgeTransaction : IDisposable, IAsyncDisposable
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="sql">The sql value.</param>
/// <param name="parameters">The parameters value.</param>
/// <param name="timeoutSeconds">The timeoutSeconds value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    IEnumerable<T> Query<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the Execute operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the Execute operation.</returns>
    int Execute(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the ExecuteAsync operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ExecuteAsync operation.</returns>
    Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <returns>The result of the T operation.</returns>
    T? ExecuteScalar<T>(string sql, object? parameters = null, int? timeoutSeconds = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="timeoutSeconds">The timeoutSeconds value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task<T?> ExecuteScalarAsync<T>(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the Commit operation.
    /// </summary>
    void Commit();
    /// <summary>
    /// Defines the CommitAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the CommitAsync operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the Rollback operation.
    /// </summary>
    void Rollback();
    /// <summary>
    /// Defines the RollbackAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RollbackAsync operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IForgeGridReader : IDisposable
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    IEnumerable<T> Read<T>();
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    Task<IReadOnlyList<T>> ReadAsync<T>();
}

public interface IForgeDiagnostics
/// <summary>
/// Defines the Analyze operation.
/// </summary>
/// <param name="sql">The sql value.</param>
/// <returns>The result of the Analyze operation.</returns>
{
    /// <summary>
    /// Defines the Analyze operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the Analyze operation.</returns>
    ForgeQueryAnalysis Analyze(string sql);
}

public interface IForgeQueryAnalyzer
/// <summary>
/// Defines the Analyze operation.
/// </summary>
/// <param name="sql">The sql value.</param>
/// <returns>The result of the Analyze operation.</returns>
{
    /// <summary>
    /// Defines the Analyze operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the Analyze operation.</returns>
    ForgeQueryAnalysis Analyze(string sql);
}

public interface IForgeEntityMetadataResolver
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    ForgeEntityMetadata Resolve<T>();
    /// <summary>
    /// Defines the Resolve operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the Resolve operation.</returns>
    ForgeEntityMetadata Resolve(Type type);
}

public interface IForgeDatabaseProvider
/// <summary>
/// Defines the CreateConnection operation.
/// </summary>
/// <param name="connectionString">The connectionString value.</param>
/// <returns>The result of the CreateConnection operation.</returns>
{
    /// <summary>
    /// Defines the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    string ProviderName { get; }
    /// <summary>
    /// Defines the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    ForgeSqlDialect Dialect { get; }
    /// <summary>
    /// Defines the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    ForgeProviderCapabilities Capabilities { get; }

/// <summary>

/// Defines the CreateConnection operation.

/// </summary>

/// <param name="connectionString">The connectionString value.</param>

/// <returns>The result of the CreateConnection operation.</returns>

    /// <summary>
    /// Defines the CreateConnection operation.
    /// </summary>
    /// <param name="connectionString">The connectionString value.</param>
    /// <returns>The result of the CreateConnection operation.</returns>
    DbConnection CreateConnection(string connectionString);
    /// <summary>
    /// Defines the BuildGetById operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="id">The id value.</param>
    /// <returns>The result of the BuildGetById operation.</returns>
    ForgeCommand BuildGetById(ForgeEntityMetadata entity, object id);
    /// <summary>
    /// Defines the BuildGetByCode operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="code">The code value.</param>
    /// <returns>The result of the BuildGetByCode operation.</returns>
    ForgeCommand BuildGetByCode(ForgeEntityMetadata entity, string code);
    /// <summary>
    /// Defines the BuildGetByIds operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="ids">The ids value.</param>
    /// <returns>The result of the BuildGetByIds operation.</returns>
    ForgeCommand BuildGetByIds(ForgeEntityMetadata entity, IReadOnlyCollection<int> ids);
    /// <summary>
    /// Defines the BuildInsert operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="entityInstance">The entityInstance value.</param>
    /// <returns>The result of the BuildInsert operation.</returns>
    ForgeCommand BuildInsert(ForgeEntityMetadata entity, object entityInstance);
    /// <summary>
    /// Defines the BuildUpdate operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="entityInstance">The entityInstance value.</param>
    /// <returns>The result of the BuildUpdate operation.</returns>
    ForgeCommand BuildUpdate(ForgeEntityMetadata entity, object entityInstance);
    /// <summary>
    /// Defines the BuildDelete operation.
    /// </summary>
    /// <param name="entity">The entity value.</param>
    /// <param name="id">The id value.</param>
    /// <returns>The result of the BuildDelete operation.</returns>
    ForgeCommand BuildDelete(ForgeEntityMetadata entity, object id);
    /// <summary>
    /// Defines the BuildPage operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the BuildPage operation.</returns>
    ForgeCommand BuildPage(ForgePageRequest request);
    /// <summary>
    /// Defines the BuildCount operation.
    /// </summary>
    /// <param name="baseSql">The baseSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the BuildCount operation.</returns>
    ForgeCommand BuildCount(string baseSql, object? parameters = null);
    /// <summary>
    /// Defines the BuildBulkDelete operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="ids">The ids value.</param>
    /// <returns>The result of the BuildBulkDelete operation.</returns>
    ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids);
    /// <summary>
    /// Defines the BuildFunctionScalar operation.
    /// </summary>
    /// <param name="functionName">The functionName value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the BuildFunctionScalar operation.</returns>
    ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task BulkInsertAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task BulkUpdateAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="connection">The connection value.</param>
    /// <param name="tableName">The tableName value.</param>
    /// <param name="rows">The rows value.</param>
    /// <param name="keyColumn">The keyColumn value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    Task BulkMergeAsync<T>(DbConnection connection, string tableName, IReadOnlyCollection<T> rows, string keyColumn, CancellationToken cancellationToken = default);
}
