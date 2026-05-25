using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

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
    ValueTask<bool> AnyAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default);
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
    ValueTask<TParent?> FirstOrDefaultAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default);
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
    ValueTask<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default);
}
