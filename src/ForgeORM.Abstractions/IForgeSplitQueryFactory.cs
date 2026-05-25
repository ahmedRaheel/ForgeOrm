using System;
using System.Data.Common;
using System.Linq.Expressions;

namespace ForgeORM.Abstractions;

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
