using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

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
    ValueTask<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default);
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
    ValueTask<T?> GetByCodeAsync<T>(string code, CancellationToken cancellationToken = default);
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
    ValueTask<IReadOnlyList<T>> GetByIdsAsync<T>(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
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
    ValueTask<int> InsertAsync<T>(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple independent entities in a single transactional operation.
    /// Use this when the rows do not form a parent-child aggregate graph.
    /// </summary>
    /// <typeparam name="T">The entity type to insert.</typeparam>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of inserted rows.</returns>
    ValueTask<int> InsertManyAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts an aggregate root and every public child collection automatically in one transaction.
    /// The parent is inserted first, the generated parent key is copied into matching child foreign keys,
    /// then child rows are inserted recursively.
    /// </summary>
    /// <typeparam name="T">The aggregate root entity type.</typeparam>
    /// <param name="entity">The parent entity containing child collection properties.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The same entity instance after key propagation.</returns>
    ValueTask<T> InsertGraphAsync<T>(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an aggregate root and synchronizes its child collections in one transaction.
    /// Existing children are updated, new children are inserted, and missing children can optionally be deleted.
    /// </summary>
    /// <typeparam name="T">The aggregate root entity type.</typeparam>
    /// <param name="entity">The parent entity containing the desired graph state.</param>
    /// <param name="deleteMissingChildren">When true, child rows not present in the supplied graph are deleted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of affected rows.</returns>
    ValueTask<int> UpdateGraphAsync<T>(T entity, bool deleteMissingChildren = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a parent row and selected child collections into a single aggregate object.
    /// Include names must match public collection property names such as Items, Payments, or Notes.
    /// </summary>
    /// <typeparam name="T">The aggregate root entity type.</typeparam>
    /// <param name="id">The parent primary-key value.</param>
    /// <param name="includes">Collection property names to load.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The parent with requested child collections, or null when the parent is not found.</returns>
    ValueTask<T?> GetGraphAsync<T>(object id, IEnumerable<string>? includes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a parent row and its child rows in one transaction.
    /// Child tables are deleted first to satisfy foreign-key constraints, then the parent is deleted.
    /// </summary>
    /// <typeparam name="T">The aggregate root entity type.</typeparam>
    /// <param name="id">The parent primary-key value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The number of affected rows.</returns>
    ValueTask<int> DeleteGraphAsync<T>(object id, CancellationToken cancellationToken = default);
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
    ValueTask<int> UpdateAsync<T>(T entity, CancellationToken cancellationToken = default);
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
    ValueTask<int> DeleteAsync<T>(object id, CancellationToken cancellationToken = default);
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
    ValueTask<ForgePagedResult<T>> PageAsync<T>(ForgePageRequest request, CancellationToken cancellationToken = default);
}
