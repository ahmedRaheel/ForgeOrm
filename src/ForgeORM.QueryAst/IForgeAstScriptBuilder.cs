using System;
using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public interface IForgeAstScriptBuilder
/// <summary>
/// Defines the WithCte operation.
/// </summary>
/// <param name="name">The name value.</param>
/// <param name="sql">The sql value.</param>
/// <returns>The result of the WithCte operation.</returns>
{
    /// <summary>
    /// Defines the WithCte operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WithCte operation.</returns>
    IForgeAstScriptBuilder WithCte(string name, string sql);
    /// <summary>
    /// Defines the CreateTempTable operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="configure">The configure value.</param>
    /// <returns>The result of the CreateTempTable operation.</returns>
    IForgeAstScriptBuilder CreateTempTable(string name, Action<IForgeAstTempTableBuilder> configure);
    /// <summary>
    /// Defines the InsertIntoTemp operation.
    /// </summary>
    /// <param name="tempTable">The tempTable value.</param>
    /// <param name="selectSql">The selectSql value.</param>
    /// <returns>The result of the InsertIntoTemp operation.</returns>
    IForgeAstScriptBuilder InsertIntoTemp(string tempTable, string selectSql);
    /// <summary>
    /// Defines the Statement operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the Statement operation.</returns>
    IForgeAstScriptBuilder Statement(string sql);
    /// <summary>
    /// Defines the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Render operation.</returns>
    ForgeRenderedSql Render(IForgeDatabaseProvider provider);
}
