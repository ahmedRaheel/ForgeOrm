using System;
using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public interface IForgeDynamicQueryBuilder
/// <summary>
/// Defines the Select operation.
/// </summary>
/// <param name="stringcolumns">The stringcolumns value.</param>
/// <returns>The result of the Select operation.</returns>
{
    /// <summary>
    /// Defines the Select operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the Select operation.</returns>
    IForgeDynamicSelectBuilder Select(params string[] columns);
    /// <summary>
    /// Defines the SelectAll operation.
    /// </summary>
    /// <returns>The result of the SelectAll operation.</returns>
    IForgeDynamicSelectBuilder SelectAll();
}
