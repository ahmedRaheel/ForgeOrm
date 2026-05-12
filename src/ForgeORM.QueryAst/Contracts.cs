using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public interface IForgeAstSelectBuilder<T>
{
    IForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns);
    IForgeAstSelectBuilder<T> Columns(params string[] columns);
    IForgeAstSelectBuilder<T> Distinct();
    IForgeAstSelectBuilder<T> From(string? tableName = null);
    IForgeAstSelectBuilder<T> As(string alias);

    IForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate);
    IForgeAstSelectBuilder<T> WhereSql(string condition, object? parameters = null);
    IForgeAstSelectBuilder<T> And(Expression<Func<T, bool>> predicate);
    IForgeAstSelectBuilder<T> AndSql(string condition, object? parameters = null);
    IForgeAstSelectBuilder<T> Or(Expression<Func<T, bool>> predicate);
    IForgeAstSelectBuilder<T> OrSql(string condition, object? parameters = null);

    IForgeAstSelectBuilder<T> Join(string table, string on);
    IForgeAstSelectBuilder<T> InnerJoin(string table, string on);
    IForgeAstSelectBuilder<T> LeftJoin(string table, string on);
    IForgeAstSelectBuilder<T> RightJoin(string table, string on);
    IForgeAstSelectBuilder<T> FullJoin(string table, string on);
    IForgeAstSelectBuilder<T> CrossJoin(string table);
    IForgeAstSelectBuilder<T> CrossApply(string tableExpression, string alias);
    IForgeAstSelectBuilder<T> OuterApply(string tableExpression, string alias);

    IForgeAstSelectBuilder<T> WithCte(string name, string sql);
    IForgeAstSelectBuilder<T> WithCte(ForgeCte cte);

    IForgeAstSelectBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns);
    IForgeAstSelectBuilder<T> GroupBy(params string[] columns);
    IForgeAstSelectBuilder<T> HavingSql(string condition);

    IForgeAstSelectBuilder<T> OrderBy(Expression<Func<T, object>> column);
    IForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column);
    IForgeAstSelectBuilder<T> OrderBySql(string orderBy);

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

public interface IForgeDynamicQueryBuilder
{
    IForgeDynamicSelectBuilder Select(params string[] columns);
    IForgeDynamicSelectBuilder SelectAll();
}

public interface IForgeDynamicSelectBuilder
{
    IForgeDynamicSelectBuilder Distinct();
    IForgeDynamicSelectBuilder From(string table);
    IForgeDynamicSelectBuilder Where(string condition, object? parameters = null);
    IForgeDynamicSelectBuilder And(string condition, object? parameters = null);
    IForgeDynamicSelectBuilder Or(string condition, object? parameters = null);
    IForgeDynamicSelectBuilder Join(string table, string on);
    IForgeDynamicSelectBuilder InnerJoin(string table, string on);
    IForgeDynamicSelectBuilder LeftJoin(string table, string on);
    IForgeDynamicSelectBuilder RightJoin(string table, string on);
    IForgeDynamicSelectBuilder FullJoin(string table, string on);
    IForgeDynamicSelectBuilder CrossJoin(string table);
    IForgeDynamicSelectBuilder CrossApply(string tableExpression, string alias);
    IForgeDynamicSelectBuilder OuterApply(string tableExpression, string alias);
    IForgeDynamicSelectBuilder WithCte(string name, string sql);
    IForgeDynamicSelectBuilder GroupBy(params string[] columns);
    IForgeDynamicSelectBuilder Having(string condition);
    IForgeDynamicSelectBuilder OrderBy(string orderBy);
    IForgeDynamicSelectBuilder Skip(int rows);
    IForgeDynamicSelectBuilder Take(int rows);
    ForgeRenderedSql Render(IForgeDatabaseProvider provider);
    ForgeRenderedSql Build(IForgeDatabaseProvider provider);
}

public sealed record ForgeRenderedSql(string Sql, object? Parameters = null);
public sealed record ForgeCte(string Name, string Sql);
public sealed record ForgeTempColumn(string Name, string DbType, bool Nullable);

public sealed class ForgeTempTable
{
    public required string Name { get; init; }
    public List<ForgeTempColumn> Columns { get; init; } = [];
    public List<string> PrimaryKeyColumns { get; init; } = [];
}
