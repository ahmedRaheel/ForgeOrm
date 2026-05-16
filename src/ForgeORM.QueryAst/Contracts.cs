using ForgeORM.Abstractions;
using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

public interface IForgeAstSelectBuilder<T>
/// <summary>
/// Defines the Columns operation.
/// </summary>
/// <param name="object">The object value.</param>
/// <returns>The result of the Columns operation.</returns>
{
    /// <summary>
    /// Defines the Columns operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the Columns operation.</returns>
    IForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns);
    /// <summary>
    /// Defines the Columns operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the Columns operation.</returns>
    IForgeAstSelectBuilder<T> Columns(params string[] columns);
    /// <summary>
    /// Defines the ColumnsSql operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the ColumnsSql operation.</returns>
    IForgeAstSelectBuilder<T> ColumnsSql(params string[] columns);
    /// <summary>
    /// Defines the Distinct operation.
    /// </summary>
    /// <returns>The result of the Distinct operation.</returns>
    IForgeAstSelectBuilder<T> Distinct();
    /// <summary>
    /// Defines the From operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <returns>The result of the From operation.</returns>
    IForgeAstSelectBuilder<T> From(string? tableName = null);
    /// <summary>
    /// Defines the As operation.
    /// </summary>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the As operation.</returns>
    IForgeAstSelectBuilder<T> As(string alias);
    /// <summary>
    /// Defines the Where operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the Where operation.</returns>
    IForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Defines the WhereSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    IForgeAstSelectBuilder<T> WhereSql(string condition, object? parameters = null);
    /// <summary>
    /// Defines the WhereIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the WhereIf operation.</returns>
    IForgeAstSelectBuilder<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Defines the WhereSqlIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="sqlCondition">The sqlCondition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSqlIf operation.</returns>
    IForgeAstSelectBuilder<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null);
    /// <summary>
    /// Defines the And operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the And operation.</returns>
    IForgeAstSelectBuilder<T> And(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Defines the AndSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the AndSql operation.</returns>
    IForgeAstSelectBuilder<T> AndSql(string condition, object? parameters = null);
    /// <summary>
    /// Defines the Or operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the Or operation.</returns>
    IForgeAstSelectBuilder<T> Or(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Defines the OrSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the OrSql operation.</returns>
    IForgeAstSelectBuilder<T> OrSql(string condition, object? parameters = null);
    /// <summary>
    /// Defines the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    IForgeAstSelectBuilder<T> Join<TJoin>(Expression<Func<T, TJoin, bool>> on);
    /// <summary>
    /// Defines the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    IForgeAstSelectBuilder<T> InnerJoin<TJoin>(Expression<Func<T, TJoin, bool>> on);
    /// <summary>
    /// Defines the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    IForgeAstSelectBuilder<T> LeftJoin<TJoin>(Expression<Func<T, TJoin, bool>> on);
    /// <summary>
    /// Defines the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    IForgeAstSelectBuilder<T> RightJoin<TJoin>(Expression<Func<T, TJoin, bool>> on);
    /// <summary>
    /// Defines the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    IForgeAstSelectBuilder<T> FullJoin<TJoin>(Expression<Func<T, TJoin, bool>> on);
    /// <summary>
    /// Defines the Join operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the Join operation.</returns>
    IForgeAstSelectBuilder<T> Join(string table, string on);
    /// <summary>
    /// Defines the JoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the JoinSql operation.</returns>
    IForgeAstSelectBuilder<T> JoinSql(string table, string on);
    /// <summary>
    /// Defines the InnerJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the InnerJoin operation.</returns>
    IForgeAstSelectBuilder<T> InnerJoin(string table, string on);
    /// <summary>
    /// Defines the InnerJoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the InnerJoinSql operation.</returns>
    IForgeAstSelectBuilder<T> InnerJoinSql(string table, string on);
    /// <summary>
    /// Defines the LeftJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the LeftJoin operation.</returns>
    IForgeAstSelectBuilder<T> LeftJoin(string table, string on);
    /// <summary>
    /// Defines the LeftJoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the LeftJoinSql operation.</returns>
    IForgeAstSelectBuilder<T> LeftJoinSql(string table, string on);
    /// <summary>
    /// Defines the RightJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the RightJoin operation.</returns>
    IForgeAstSelectBuilder<T> RightJoin(string table, string on);
    /// <summary>
    /// Defines the RightJoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the RightJoinSql operation.</returns>
    IForgeAstSelectBuilder<T> RightJoinSql(string table, string on);
    /// <summary>
    /// Defines the FullJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the FullJoin operation.</returns>
    IForgeAstSelectBuilder<T> FullJoin(string table, string on);
    /// <summary>
    /// Defines the FullJoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the FullJoinSql operation.</returns>
    IForgeAstSelectBuilder<T> FullJoinSql(string table, string on);
    /// <summary>
    /// Defines the CrossJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the CrossJoin operation.</returns>
    IForgeAstSelectBuilder<T> CrossJoin(string table);
    /// <summary>
    /// Defines the CrossApply operation.
    /// </summary>
    /// <param name="tableExpression">The tableExpression value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the CrossApply operation.</returns>
    IForgeAstSelectBuilder<T> CrossApply(string tableExpression, string alias);
    /// <summary>
    /// Defines the OuterApply operation.
    /// </summary>
    /// <param name="tableExpression">The tableExpression value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the OuterApply operation.</returns>
    IForgeAstSelectBuilder<T> OuterApply(string tableExpression, string alias);
    /// <summary>
    /// Defines the WithCte operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WithCte operation.</returns>
    IForgeAstSelectBuilder<T> WithCte(string name, string sql);
    /// <summary>
    /// Defines the WithCte operation.
    /// </summary>
    /// <param name="cte">The cte value.</param>
    /// <returns>The result of the WithCte operation.</returns>
    IForgeAstSelectBuilder<T> WithCte(ForgeCte cte);
    /// <summary>
    /// Defines the GroupBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    IForgeAstSelectBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns);
    /// <summary>
    /// Defines the GroupBy operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    IForgeAstSelectBuilder<T> GroupBy(params string[] columns);
    /// <summary>
    /// Defines the HavingSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <returns>The result of the HavingSql operation.</returns>
    IForgeAstSelectBuilder<T> HavingSql(string condition);
    /// <summary>
    /// Defines the OrderBy operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    IForgeAstSelectBuilder<T> OrderBy(Expression<Func<T, object>> column);
    /// <summary>
    /// Defines the OrderByDescending operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
    IForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column);
    /// <summary>
    /// Defines the OrderBySql operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBySql operation.</returns>
    IForgeAstSelectBuilder<T> OrderBySql(string orderBy);
    /// <summary>
    /// Defines the Skip operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Skip operation.</returns>
    IForgeAstSelectBuilder<T> Skip(int rows);
    /// <summary>
    /// Defines the Take operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Take operation.</returns>
    IForgeAstSelectBuilder<T> Take(int rows);
    /// <summary>
    /// Defines the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Render operation.</returns>
    ForgeRenderedSql Render(IForgeDatabaseProvider provider);
    /// <summary>
    /// Defines the RenderCount operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the RenderCount operation.</returns>
    ForgeRenderedSql RenderCount(IForgeDatabaseProvider provider);
    /// <summary>
    /// Defines the RenderAny operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the RenderAny operation.</returns>
    ForgeRenderedSql RenderAny(IForgeDatabaseProvider provider);
}

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

public interface IForgeAstTempTableBuilder
/// <summary>
/// Defines the Column operation.
/// </summary>
/// <param name="name">The name value.</param>
/// <param name="dbType">The dbType value.</param>
/// <param name="nullable">The nullable value.</param>
/// <returns>The result of the Column operation.</returns>
{
    /// <summary>
    /// Defines the Column operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="dbType">The dbType value.</param>
    /// <param name="nullable">The nullable value.</param>
    /// <returns>The result of the Column operation.</returns>
    IForgeAstTempTableBuilder Column(string name, string dbType, bool nullable = true);
    /// <summary>
    /// Defines the PrimaryKey operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the PrimaryKey operation.</returns>
    IForgeAstTempTableBuilder PrimaryKey(params string[] columns);
    /// <summary>
    /// Defines the Build operation.
    /// </summary>
    /// <returns>The result of the Build operation.</returns>
    ForgeTempTable Build();
}

public interface IForgeDynamicQueryBuilder
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
    IForgeDynamicSelectBuilder Select(params string[] columns);
    /// <summary>
    /// Defines the SelectAll operation.
    /// </summary>
    /// <returns>The result of the SelectAll operation.</returns>
    IForgeDynamicSelectBuilder SelectAll();
}

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

public sealed record ForgeRenderedSql(string Sql, object? Parameters = null);
public sealed record ForgeCte(string Name, string Sql);
public sealed record ForgeTempColumn(string Name, string DbType, bool Nullable);

public sealed class ForgeTempTable
{
    public required string Name { get; init; }
    public List<ForgeTempColumn> Columns { get; init; } = [];
    public List<string> PrimaryKeyColumns { get; init; } = [];
}
