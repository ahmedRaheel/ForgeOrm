using System;
using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public interface IForgeDynamicSelectBuilder
/// <summary>
/// Defines the Distinct operation.
/// </summary>
/// <returns>The result of the Distinct operation.</returns>
{
    /// <summary>
    /// Defines the Distinct operation.
    /// </summary>
    /// <returns>The result of the Distinct operation.</returns>
    IForgeDynamicSelectBuilder Distinct();
    /// <summary>
    /// Defines the From operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the From operation.</returns>
    IForgeDynamicSelectBuilder From(string table);
    /// <summary>
    /// Defines the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Where operation.</returns>
    IForgeDynamicSelectBuilder Where(string condition, object? parameters = null);
    /// <summary>
    /// Defines the And operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the And operation.</returns>
    IForgeDynamicSelectBuilder And(string condition, object? parameters = null);
    /// <summary>
    /// Defines the Or operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Or operation.</returns>
    IForgeDynamicSelectBuilder Or(string condition, object? parameters = null);
    /// <summary>
    /// Defines the Join operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the Join operation.</returns>
    IForgeDynamicSelectBuilder Join(string table, string on);
    /// <summary>
    /// Defines the InnerJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the InnerJoin operation.</returns>
    IForgeDynamicSelectBuilder InnerJoin(string table, string on);
    /// <summary>
    /// Defines the LeftJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the LeftJoin operation.</returns>
    IForgeDynamicSelectBuilder LeftJoin(string table, string on);
    /// <summary>
    /// Defines the RightJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the RightJoin operation.</returns>
    IForgeDynamicSelectBuilder RightJoin(string table, string on);
    /// <summary>
    /// Defines the FullJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the FullJoin operation.</returns>
    IForgeDynamicSelectBuilder FullJoin(string table, string on);
    /// <summary>
    /// Defines the CrossJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the CrossJoin operation.</returns>
    IForgeDynamicSelectBuilder CrossJoin(string table);
    /// <summary>
    /// Defines the CrossApply operation.
    /// </summary>
    /// <param name="tableExpression">The tableExpression value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the CrossApply operation.</returns>
    IForgeDynamicSelectBuilder CrossApply(string tableExpression, string alias);
    /// <summary>
    /// Defines the OuterApply operation.
    /// </summary>
    /// <param name="tableExpression">The tableExpression value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the OuterApply operation.</returns>
    IForgeDynamicSelectBuilder OuterApply(string tableExpression, string alias);
    /// <summary>
    /// Defines the WithCte operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WithCte operation.</returns>
    IForgeDynamicSelectBuilder WithCte(string name, string sql);
    /// <summary>
    /// Defines the GroupBy operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    IForgeDynamicSelectBuilder GroupBy(params string[] columns);
    /// <summary>
    /// Defines the Having operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <returns>The result of the Having operation.</returns>
    IForgeDynamicSelectBuilder Having(string condition);
    /// <summary>
    /// Defines the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    IForgeDynamicSelectBuilder OrderBy(string orderBy);
    /// <summary>
    /// Defines the Skip operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Skip operation.</returns>
    IForgeDynamicSelectBuilder Skip(int rows);
    /// <summary>
    /// Defines the Take operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Take operation.</returns>
    IForgeDynamicSelectBuilder Take(int rows);
    /// <summary>
    /// Defines the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Render operation.</returns>
    ForgeRenderedSql Render(IForgeDatabaseProvider provider);
    /// <summary>
    /// Defines the Build operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Build operation.</returns>
    ForgeRenderedSql Build(IForgeDatabaseProvider provider);
}
