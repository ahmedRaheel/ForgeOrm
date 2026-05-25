using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryBuilder;

public interface IForgeSelectBuilder
/// <summary>
/// Defines the From operation.
/// </summary>
/// <param name="table">The table value.</param>
/// <returns>The result of the From operation.</returns>
{
    /// <summary>
    /// Defines the From operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the From operation.</returns>
    IForgeSelectBuilder From(string table);
    /// <summary>
    /// Defines the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Where operation.</returns>
    IForgeSelectBuilder Where(string condition, object? parameters = null);
    /// <summary>
    /// Defines the And operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the And operation.</returns>
    IForgeSelectBuilder And(string condition, object? parameters = null);
    /// <summary>
    /// Defines the Or operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Or operation.</returns>
    IForgeSelectBuilder Or(string condition, object? parameters = null);
    /// <summary>
    /// Defines the Join operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the Join operation.</returns>
    IForgeSelectBuilder Join(string table, string on);
    /// <summary>
    /// Defines the LeftJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the LeftJoin operation.</returns>
    IForgeSelectBuilder LeftJoin(string table, string on);
    /// <summary>
    /// Defines the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    IForgeSelectBuilder OrderBy(string orderBy);
    /// <summary>
    /// Defines the GroupBy operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    IForgeSelectBuilder GroupBy(params string[] columns);
    /// <summary>
    /// Defines the Having operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <returns>The result of the Having operation.</returns>
    IForgeSelectBuilder Having(string condition);
    /// <summary>
    /// Defines the Skip operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Skip operation.</returns>
    IForgeSelectBuilder Skip(int rows);
    /// <summary>
    /// Defines the Take operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Take operation.</returns>
    IForgeSelectBuilder Take(int rows);
    /// <summary>
    /// Defines the Build operation.
    /// </summary>
    /// <returns>The result of the Build operation.</returns>
    ForgeBuiltQuery Build();
}
