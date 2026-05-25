using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryBuilder;

public interface IForgeSelectQueryBuilder
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
    IForgeSelectBuilder Select(params string[] columns);
    /// <summary>
    /// Defines the SelectAll operation.
    /// </summary>
    /// <returns>The result of the SelectAll operation.</returns>
    IForgeSelectBuilder SelectAll();
}
