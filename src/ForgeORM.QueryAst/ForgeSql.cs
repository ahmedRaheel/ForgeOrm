using System.Linq.Expressions;
using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryAst;

public static class ForgeSql
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <returns>The result of the T operation.</returns>
    public static IForgeAstSelectBuilder<T> Select<T>() => new ForgeAstSelectBuilder<T>();
    /// <summary>
    /// Executes the Script operation.
    /// </summary>
    /// <returns>The result of the Script operation.</returns>
    public static IForgeAstScriptBuilder Script() => new ForgeAstScriptBuilder();
    /// <summary>
    /// Executes the TempTable operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The result of the TempTable operation.</returns>
    public static IForgeAstTempTableBuilder TempTable(string name) => new ForgeAstTempTableBuilder(name);
    /// <summary>
    /// Executes the Cte operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the Cte operation.</returns>
    public static ForgeCte Cte(string name, string sql) => new(name, sql);
}
