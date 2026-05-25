using System;
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

    IForgeAstSelectBuilder<T> TemporalAll();
    IForgeAstSelectBuilder<T> TemporalAsOf(DateTime asOfUtc);
    IForgeAstSelectBuilder<T> TemporalBetween(DateTime fromUtc, DateTime toUtc);
    IForgeAstSelectBuilder<T> TemporalContainedIn(DateTime fromUtc, DateTime toUtc);
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
    /// Adds a HAVING condition generated from an expression.
    /// </summary>
    /// <param name="predicate">The expression used to generate the HAVING condition.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Having(Expression<Func<T, bool>> predicate);
    /// <summary>
    /// Adds a COUNT aggregate column to the SELECT list.
    /// </summary>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Count(string alias = "Count");
    /// <summary>
    /// Adds a COUNT aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The column expression to count.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Count(Expression<Func<T, object>> column, string alias = "Count");
    /// <summary>
    /// Adds a SUM aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The numeric column expression to sum.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Sum(Expression<Func<T, object>> column, string alias = "Sum");
    /// <summary>
    /// Adds an AVG aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The numeric column expression to average.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Average(Expression<Func<T, object>> column, string alias = "Average");
    /// <summary>
    /// Adds a MIN aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The column expression used by MIN.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Min(Expression<Func<T, object>> column, string alias = "Min");
    /// <summary>
    /// Adds a MAX aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The column expression used by MAX.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Max(Expression<Func<T, object>> column, string alias = "Max");
    /// <summary>
    /// Adds a SQL aggregate expression to the SELECT list.
    /// </summary>
    /// <param name="sqlExpression">The SQL aggregate expression.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> AggregateSql(string sqlExpression, string alias);
    /// <summary>
    /// Adds a HAVING condition for a COUNT aggregate.
    /// </summary>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> HavingCount(string @operator, object value);
    /// <summary>
    /// Adds a HAVING condition for a SUM aggregate.
    /// </summary>
    /// <param name="column">The numeric column expression to sum.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> HavingSum(Expression<Func<T, object>> column, string @operator, object value);
    /// <summary>
    /// Adds a HAVING condition for an AVG aggregate.
    /// </summary>
    /// <param name="column">The numeric column expression to average.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> HavingAverage(Expression<Func<T, object>> column, string @operator, object value);
    /// <summary>
    /// Adds a HAVING condition for a MIN aggregate.
    /// </summary>
    /// <param name="column">The column expression used by MIN.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> HavingMin(Expression<Func<T, object>> column, string @operator, object value);
    /// <summary>
    /// Adds a HAVING condition for a MAX aggregate.
    /// </summary>
    /// <param name="column">The column expression used by MAX.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> HavingMax(Expression<Func<T, object>> column, string @operator, object value);
    /// <summary>
    /// Adds a SQL HAVING condition for an aggregate expression.
    /// </summary>
    /// <param name="aggregateSql">The SQL aggregate expression.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> HavingAggregateSql(string aggregateSql, string @operator, object value);
    /// <summary>
    /// Adds a UNION set operation using another expression-built query.
    /// </summary>
    /// <param name="configure">The callback that configures the second query.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Union(Action<IForgeAstSelectBuilder<T>> configure);
    /// <summary>
    /// Adds a UNION set operation using raw SQL.
    /// </summary>
    /// <param name="sql">The SQL query to union with the current query.</param>
    /// <param name="parameters">Optional SQL parameters used by the union query.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> UnionSql(string sql, object? parameters = null);
    /// <summary>
    /// Adds a UNION ALL set operation using another expression-built query.
    /// </summary>
    /// <param name="configure">The callback that configures the second query.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> UnionAll(Action<IForgeAstSelectBuilder<T>> configure);
    /// <summary>
    /// Adds a UNION ALL set operation using raw SQL.
    /// </summary>
    /// <param name="sql">The SQL query to union with the current query.</param>
    /// <param name="parameters">Optional SQL parameters used by the union query.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> UnionAllSql(string sql, object? parameters = null);
    /// <summary>
    /// Adds an INTERSECT set operation using another expression-built query.
    /// </summary>
    /// <param name="configure">The callback that configures the second query.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Intersect(Action<IForgeAstSelectBuilder<T>> configure);
    /// <summary>
    /// Adds an INTERSECT set operation using raw SQL.
    /// </summary>
    /// <param name="sql">The SQL query to intersect with the current query.</param>
    /// <param name="parameters">Optional SQL parameters used by the intersect query.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> IntersectSql(string sql, object? parameters = null);
    /// <summary>
    /// Adds an EXCEPT set operation using another expression-built query.
    /// </summary>
    /// <param name="configure">The callback that configures the second query.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> Except(Action<IForgeAstSelectBuilder<T>> configure);
    /// <summary>
    /// Adds an EXCEPT set operation using raw SQL.
    /// </summary>
    /// <param name="sql">The SQL query to subtract from the current query.</param>
    /// <param name="parameters">Optional SQL parameters used by the except query.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> ExceptSql(string sql, object? parameters = null);
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
    /// <summary>
    /// Adds an IN condition against the entity key column.
    /// </summary>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values used by the IN condition.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> WhereIds<TKey>(IEnumerable<TKey> ids);
    /// <summary>
    /// Adds an IN condition against a key column selected by expression.
    /// </summary>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values used by the IN condition.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> WhereIds<TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids);
    /// <summary>
    /// Adds an IN condition against an explicit SQL key column.
    /// </summary>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL key column used by the IN condition.</param>
    /// <param name="ids">The key values used by the IN condition.</param>
    /// <returns>The current AST select builder.</returns>
    IForgeAstSelectBuilder<T> WhereIdsSql<TKey>(string keyColumn, IEnumerable<TKey> ids);
    /// <summary>
    /// Renders the current AST query as a DELETE command using the configured filters.
    /// </summary>
    /// <param name="provider">The database provider used to render provider-aware SQL.</param>
    /// <returns>The rendered DELETE SQL and parameters.</returns>
    ForgeRenderedSql RenderDelete(IForgeDatabaseProvider provider);
    /// <summary>
    /// Renders the current AST query as an UPDATE command using the configured filters.
    /// </summary>
    /// <param name="provider">The database provider used to render provider-aware SQL.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <returns>The rendered UPDATE SQL and parameters.</returns>
    ForgeRenderedSql RenderUpdate(IForgeDatabaseProvider provider, object values);
}
