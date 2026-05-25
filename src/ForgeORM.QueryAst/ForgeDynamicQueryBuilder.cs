using ForgeORM.Abstractions;
using System.Text;

namespace ForgeORM.QueryAst;

public sealed class ForgeDynamicQueryBuilder : IForgeDynamicQueryBuilder
{
    /// <summary>
    /// Executes the Select operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the Select operation.</returns>
    public IForgeDynamicSelectBuilder Select(params string[] columns) => new ForgeDynamicSelectBuilder(columns.Length == 0 ? ["*"] : columns);
    /// <summary>
    /// Executes the SelectAll operation.
    /// </summary>
    /// <returns>The result of the SelectAll operation.</returns>
    public IForgeDynamicSelectBuilder SelectAll() => new ForgeDynamicSelectBuilder(["*"]);
}
