using System.Linq.Expressions;
using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryAst;

public static class ForgeSql
{
    /// <summary>
    /// Initializes or executes the Select operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public static IForgeAstSelectBuilder<T> Select<T>() => new ForgeAstSelectBuilder<T>();
    /// <summary>
    /// Initializes or executes the Script operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public static IForgeAstScriptBuilder Script() => new ForgeAstScriptBuilder();
    /// <summary>
    /// Initializes or executes the TempTable operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <returns>The operation result.</returns>
    public static IForgeAstTempTableBuilder TempTable(string name) => new ForgeAstTempTableBuilder(name);
    /// <summary>
    /// Initializes or executes the Cte operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeCte Cte(string name, string sql) => new(name, sql);
}
