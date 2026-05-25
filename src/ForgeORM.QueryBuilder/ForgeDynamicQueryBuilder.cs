using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryBuilder;

public sealed class ForgeDynamicQueryBuilder : IForgeSelectQueryBuilder
{
    /// <summary>
    /// Executes the Select operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the Select operation.</returns>
    public IForgeSelectBuilder Select(params string[] columns) => new ForgeSelectBuilder(columns.Length == 0 ? ["*"] : columns);
    /// <summary>
    /// Executes the SelectAll operation.
    /// </summary>
    /// <returns>The result of the SelectAll operation.</returns>
    public IForgeSelectBuilder SelectAll() => new ForgeSelectBuilder(["*"]);
}
