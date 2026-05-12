using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryAst;

public interface IForgeAstSelectBuilder<T>
{
    IForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns);
    IForgeAstSelectBuilder<T> From(string? tableName = null);
    IForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate);
    IForgeAstSelectBuilder<T> WhereSql(string condition, object? parameters = null);
    IForgeAstSelectBuilder<T> Join(string table, string on);
    IForgeAstSelectBuilder<T> LeftJoin(string table, string on);
    IForgeAstSelectBuilder<T> WithCte(string name, string sql);
    IForgeAstSelectBuilder<T> WithCte(ForgeCte cte);
    IForgeAstSelectBuilder<T> OrderBy(Expression<Func<T, object>> column);
    IForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column);
    IForgeAstSelectBuilder<T> OrderBySql(string orderBy);
    IForgeAstSelectBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns);
    IForgeAstSelectBuilder<T> HavingSql(string condition);
    IForgeAstSelectBuilder<T> Skip(int rows);
    IForgeAstSelectBuilder<T> Take(int rows);
    ForgeRenderedSql Render(IForgeDatabaseProvider provider);
}

public interface IForgeAstScriptBuilder
{
    IForgeAstScriptBuilder WithCte(string name, string sql);
    IForgeAstScriptBuilder CreateTempTable(string name, Action<IForgeAstTempTableBuilder> configure);
    IForgeAstScriptBuilder InsertIntoTemp(string tempTable, string selectSql);
    IForgeAstScriptBuilder Statement(string sql);
    ForgeRenderedSql Render(IForgeDatabaseProvider provider);
}

public interface IForgeAstTempTableBuilder
{
    IForgeAstTempTableBuilder Column(string name, string dbType, bool nullable = true);
    IForgeAstTempTableBuilder PrimaryKey(params string[] columns);
    ForgeTempTable Build();
}

public sealed record ForgeRenderedSql(string Sql, object? Parameters = null);
public sealed record ForgeCte(string Name, string Sql);

public sealed class ForgeTempTable
{
    public required string Name { get; init; }
    public List<ForgeTempColumn> Columns { get; init; } = [];
    public List<string> PrimaryKeyColumns { get; init; } = [];
}

public sealed record ForgeTempColumn(string Name, string DbType, bool Nullable);
