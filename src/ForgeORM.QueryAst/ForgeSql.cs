using System.Linq.Expressions;
using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryAst;

public static class ForgeSql
{
    public static IForgeAstSelectBuilder<T> Select<T>() => new ForgeAstSelectBuilder<T>();
    public static IForgeAstScriptBuilder Script() => new ForgeAstScriptBuilder();
    public static IForgeAstTempTableBuilder TempTable(string name) => new ForgeAstTempTableBuilder(name);
    public static ForgeCte Cte(string name, string sql) => new(name, sql);
}
