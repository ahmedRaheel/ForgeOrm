using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

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
    ValueTask<IForgeTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
