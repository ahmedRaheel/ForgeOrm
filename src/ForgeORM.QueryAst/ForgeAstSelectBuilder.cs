using System;
using ForgeORM.Abstractions;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ForgeORM.QueryAst;

internal sealed partial class ForgeAstSelectBuilder<T> : IForgeAstSelectBuilder<T>
{
    private readonly List<string> _columns = [];
    private readonly List<string> _joins = [];
    private readonly List<string> _where = [];
    private readonly List<string> _groupBy = [];
    private readonly List<ForgeCte> _ctes = [];
    private readonly List<ForgeSetOperation> _setOperations = [];
    private readonly Dictionary<string, object?> _parameters = [];

    private int _parameterIndex;
    private string? _table;
    private string? _alias;
    private string? _orderBy;
    private string? _having;
    private bool _distinct;
    private int? _skip;
    private int? _take;
    private object? _rawParameters;
    private string? _temporalClause;

    /// <summary>
    /// Executes the Columns operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the Columns operation.</returns>
    public IForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns)
    {
        _columns.AddRange(columns.Select(ForgeAstExpression.MemberName));
        return this;
    }

    /// <summary>
    /// Executes the Columns operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the Columns operation.</returns>
    public IForgeAstSelectBuilder<T> Columns(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Executes the ColumnsSql operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the ColumnsSql operation.</returns>
    public IForgeAstSelectBuilder<T> ColumnsSql(params string[] columns) => Columns(columns);

    /// <summary>
    /// Executes the Distinct operation.
    /// </summary>
    /// <returns>The result of the Distinct operation.</returns>
    public IForgeAstSelectBuilder<T> Distinct()
    {
        _distinct = true;
        return this;
    }

    /// <summary>
    /// Executes the From operation.
    /// </summary>
    /// <param name="tableName">The tableName value.</param>
    /// <returns>The result of the From operation.</returns>
    public IForgeAstSelectBuilder<T> From(string? tableName = null)
    {
        _table = tableName ?? ResolveTableName(typeof(T));
        return this;
    }


    public IForgeAstSelectBuilder<T> TemporalAll()
    {
        _temporalClause = "FOR SYSTEM_TIME ALL";
        return this;
    }

    public IForgeAstSelectBuilder<T> TemporalAsOf(DateTime asOfUtc)
    {
        _temporalClause = "FOR SYSTEM_TIME AS OF @TemporalAsOf";
        _parameters["TemporalAsOf"] = asOfUtc;
        return this;
    }

    public IForgeAstSelectBuilder<T> TemporalBetween(DateTime fromUtc, DateTime toUtc)
    {
        _temporalClause = "FOR SYSTEM_TIME BETWEEN @TemporalFrom AND @TemporalTo";
        _parameters["TemporalFrom"] = fromUtc;
        _parameters["TemporalTo"] = toUtc;
        return this;
    }

    public IForgeAstSelectBuilder<T> TemporalContainedIn(DateTime fromUtc, DateTime toUtc)
    {
        _temporalClause = "FOR SYSTEM_TIME CONTAINED IN (@TemporalFrom, @TemporalTo)";
        _parameters["TemporalFrom"] = fromUtc;
        _parameters["TemporalTo"] = toUtc;
        return this;
    }

    /// <summary>
    /// Executes the As operation.
    /// </summary>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the As operation.</returns>
    public IForgeAstSelectBuilder<T> As(string alias)
    {
        _alias = alias;
        return this;
    }

    /// <summary>
    /// Executes the Where operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the Where operation.</returns>
    public IForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        var result = ForgeAstExpression.Translate(predicate, _parameterIndex);
        _parameterIndex += result.Parameters.Count;
        MergeParameters(result.Parameters);
        _where.Add(result.Sql);
        return this;
    }

    /// <summary>
    /// Executes the WhereSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    public IForgeAstSelectBuilder<T> WhereSql(string condition, object? parameters = null)
    {
        _where.Add(condition);
        MergeRawParameters(parameters);
        return this;
    }

    /// <summary>
    /// Executes the WhereIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the WhereIf operation.</returns>
    public IForgeAstSelectBuilder<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
        => condition ? Where(predicate) : this;

    /// <summary>
    /// Executes the WhereSqlIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="sqlCondition">The sqlCondition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the WhereSqlIf operation.</returns>
    public IForgeAstSelectBuilder<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null)
        => condition ? WhereSql(sqlCondition, parameters) : this;

    /// <summary>
    /// Executes the And operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the And operation.</returns>
    public IForgeAstSelectBuilder<T> And(Expression<Func<T, bool>> predicate) => Where(predicate);
    /// <summary>
    /// Executes the AndSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the AndSql operation.</returns>
    public IForgeAstSelectBuilder<T> AndSql(string condition, object? parameters = null) => WhereSql(condition, parameters);

    /// <summary>
    /// Executes the Or operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the Or operation.</returns>
    public IForgeAstSelectBuilder<T> Or(Expression<Func<T, bool>> predicate)
    {
        var result = ForgeAstExpression.Translate(predicate, _parameterIndex);
        _parameterIndex += result.Parameters.Count;
        MergeParameters(result.Parameters);

        if (_where.Count == 0)
            _where.Add(result.Sql);
        else
            _where[^1] = $"({_where[^1]}) OR ({result.Sql})";

        return this;
    }

    /// <summary>
    /// Executes the OrSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the OrSql operation.</returns>
    public IForgeAstSelectBuilder<T> OrSql(string condition, object? parameters = null)
    {
        if (_where.Count == 0)
            _where.Add(condition);
        else
            _where[^1] = $"({_where[^1]}) OR ({condition})";

        MergeRawParameters(parameters);

        return this;
    }

    /// <summary>
    /// Executes the Join operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the Join operation.</returns>
    public IForgeAstSelectBuilder<T> Join(string table, string on) => InnerJoin(table, on);
    /// <summary>
    /// Executes the JoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the JoinSql operation.</returns>
    public IForgeAstSelectBuilder<T> JoinSql(string table, string on) => InnerJoinSql(table, on);
    /// <summary>
    /// Executes the InnerJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the InnerJoin operation.</returns>
    public IForgeAstSelectBuilder<T> InnerJoin(string table, string on) => AddJoin($"INNER JOIN {table} ON {on}");
    /// <summary>
    /// Executes the InnerJoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the InnerJoinSql operation.</returns>
    public IForgeAstSelectBuilder<T> InnerJoinSql(string table, string on) => InnerJoin(table, on);
    /// <summary>
    /// Executes the LeftJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the LeftJoin operation.</returns>
    public IForgeAstSelectBuilder<T> LeftJoin(string table, string on) => AddJoin($"LEFT JOIN {table} ON {on}");
    /// <summary>
    /// Executes the LeftJoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the LeftJoinSql operation.</returns>
    public IForgeAstSelectBuilder<T> LeftJoinSql(string table, string on) => LeftJoin(table, on);
    /// <summary>
    /// Executes the RightJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the RightJoin operation.</returns>
    public IForgeAstSelectBuilder<T> RightJoin(string table, string on) => AddJoin($"RIGHT JOIN {table} ON {on}");
    /// <summary>
    /// Executes the RightJoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the RightJoinSql operation.</returns>
    public IForgeAstSelectBuilder<T> RightJoinSql(string table, string on) => RightJoin(table, on);
    /// <summary>
    /// Executes the FullJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the FullJoin operation.</returns>
    public IForgeAstSelectBuilder<T> FullJoin(string table, string on) => AddJoin($"FULL OUTER JOIN {table} ON {on}");
    /// <summary>
    /// Executes the FullJoinSql operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the FullJoinSql operation.</returns>
    public IForgeAstSelectBuilder<T> FullJoinSql(string table, string on) => FullJoin(table, on);
    /// <summary>
    /// Executes the CrossJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the CrossJoin operation.</returns>
    public IForgeAstSelectBuilder<T> CrossJoin(string table) => AddJoin($"CROSS JOIN {table}");
    /// <summary>
    /// Executes the CrossApply operation.
    /// </summary>
    /// <param name="tableExpression">The tableExpression value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the CrossApply operation.</returns>
    public IForgeAstSelectBuilder<T> CrossApply(string tableExpression, string alias) => AddJoin($"CROSS APPLY ({tableExpression}) {alias}");
    /// <summary>
    /// Executes the OuterApply operation.
    /// </summary>
    /// <param name="tableExpression">The tableExpression value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the OuterApply operation.</returns>
    public IForgeAstSelectBuilder<T> OuterApply(string tableExpression, string alias) => AddJoin($"OUTER APPLY ({tableExpression}) {alias}");

    /// <summary>
    /// Executes the WithCte operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WithCte operation.</returns>
    public IForgeAstSelectBuilder<T> WithCte(string name, string sql)
    {
        _ctes.Add(new ForgeCte(name, sql));
        return this;
    }

    /// <summary>
    /// Executes the WithCte operation.
    /// </summary>
    /// <param name="cte">The cte value.</param>
    /// <returns>The result of the WithCte operation.</returns>
    public IForgeAstSelectBuilder<T> WithCte(ForgeCte cte)
    {
        _ctes.Add(cte);
        return this;
    }

    /// <summary>
    /// Executes the GroupBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    public IForgeAstSelectBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns)
    {
        _groupBy.AddRange(columns.Select(ForgeAstExpression.MemberName));
        return this;
    }

    /// <summary>
    /// Executes the GroupBy operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    public IForgeAstSelectBuilder<T> GroupBy(params string[] columns)
    {
        _groupBy.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Executes the HavingSql operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <returns>The result of the HavingSql operation.</returns>
    public IForgeAstSelectBuilder<T> HavingSql(string condition)
    {
        _having = condition;
        return this;
    }

    /// <summary>
    /// Adds a HAVING condition generated from an expression.
    /// </summary>
    /// <param name="predicate">The expression used to generate the HAVING condition.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Having(Expression<Func<T, bool>> predicate)
    {
        var result = ForgeAstExpression.Translate(predicate, _parameterIndex);
        _parameterIndex += result.Parameters.Count;
        MergeParameters(result.Parameters);
        _having = result.Sql;
        return this;
    }

    /// <summary>
    /// Adds a COUNT aggregate column to the SELECT list.
    /// </summary>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Count(string alias = "Count")
        => AggregateSql("COUNT(1)", alias);

    /// <summary>
    /// Adds a COUNT aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The column expression to count.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Count(Expression<Func<T, object>> column, string alias = "Count")
        => AggregateSql($"COUNT({ForgeAstExpression.MemberName(column)})", alias);

    /// <summary>
    /// Adds a SUM aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The numeric column expression to sum.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Sum(Expression<Func<T, object>> column, string alias = "Sum")
        => AggregateSql($"SUM({ForgeAstExpression.MemberName(column)})", alias);

    /// <summary>
    /// Adds an AVG aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The numeric column expression to average.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Average(Expression<Func<T, object>> column, string alias = "Average")
        => AggregateSql($"AVG({ForgeAstExpression.MemberName(column)})", alias);

    /// <summary>
    /// Adds a MIN aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The column expression used by MIN.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Min(Expression<Func<T, object>> column, string alias = "Min")
        => AggregateSql($"MIN({ForgeAstExpression.MemberName(column)})", alias);

    /// <summary>
    /// Adds a MAX aggregate column for the selected expression.
    /// </summary>
    /// <param name="column">The column expression used by MAX.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Max(Expression<Func<T, object>> column, string alias = "Max")
        => AggregateSql($"MAX({ForgeAstExpression.MemberName(column)})", alias);

    /// <summary>
    /// Adds a SQL aggregate expression to the SELECT list.
    /// </summary>
    /// <param name="sqlExpression">The SQL aggregate expression, for example <c>SUM(p.Price)</c>.</param>
    /// <param name="alias">The alias assigned to the aggregate column.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> AggregateSql(string sqlExpression, string alias)
    {
        _columns.Add($"{sqlExpression} AS {alias}");
        return this;
    }

    /// <summary>
    /// Adds a HAVING condition for a COUNT aggregate.
    /// </summary>
    /// <param name="operator">The SQL comparison operator, for example <c>&gt;</c>, <c>&gt;=</c>, or <c>=</c>.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> HavingCount(string @operator, object value)
        => HavingAggregateSql("COUNT(1)", @operator, value);

    /// <summary>
    /// Adds a HAVING condition for a SUM aggregate.
    /// </summary>
    /// <param name="column">The numeric column expression to sum.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> HavingSum(Expression<Func<T, object>> column, string @operator, object value)
        => HavingAggregateSql($"SUM({ForgeAstExpression.MemberName(column)})", @operator, value);

    /// <summary>
    /// Adds a HAVING condition for an AVG aggregate.
    /// </summary>
    /// <param name="column">The numeric column expression to average.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> HavingAverage(Expression<Func<T, object>> column, string @operator, object value)
        => HavingAggregateSql($"AVG({ForgeAstExpression.MemberName(column)})", @operator, value);

    /// <summary>
    /// Adds a HAVING condition for a MIN aggregate.
    /// </summary>
    /// <param name="column">The column expression used by MIN.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> HavingMin(Expression<Func<T, object>> column, string @operator, object value)
        => HavingAggregateSql($"MIN({ForgeAstExpression.MemberName(column)})", @operator, value);

    /// <summary>
    /// Adds a HAVING condition for a MAX aggregate.
    /// </summary>
    /// <param name="column">The column expression used by MAX.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> HavingMax(Expression<Func<T, object>> column, string @operator, object value)
        => HavingAggregateSql($"MAX({ForgeAstExpression.MemberName(column)})", @operator, value);

    /// <summary>
    /// Adds a SQL HAVING condition for an aggregate expression.
    /// </summary>
    /// <param name="aggregateSql">The SQL aggregate expression, for example <c>SUM(TotalAmount)</c>.</param>
    /// <param name="operator">The SQL comparison operator.</param>
    /// <param name="value">The value compared with the aggregate result.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> HavingAggregateSql(string aggregateSql, string @operator, object value)
    {
        var parameterName = "having" + _parameterIndex++;
        _parameters[parameterName] = value;
        _having = $"{aggregateSql} {@operator} @{parameterName}";
        return this;
    }

    /// <summary>
    /// Adds a UNION set operation using another expression-built query.
    /// </summary>
    /// <param name="configure">The callback that configures the second query.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Union(Action<IForgeAstSelectBuilder<T>> configure)
        => AddSetOperation("UNION", configure);

    /// <summary>
    /// Adds a UNION set operation using raw SQL.
    /// </summary>
    /// <param name="sql">The SQL query to union with the current query.</param>
    /// <param name="parameters">Optional SQL parameters used by the union query.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> UnionSql(string sql, object? parameters = null)
        => AddSetOperationSql("UNION", sql, parameters);

    /// <summary>
    /// Adds a UNION ALL set operation using another expression-built query.
    /// </summary>
    /// <param name="configure">The callback that configures the second query.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> UnionAll(Action<IForgeAstSelectBuilder<T>> configure)
        => AddSetOperation("UNION ALL", configure);

    /// <summary>
    /// Adds a UNION ALL set operation using raw SQL.
    /// </summary>
    /// <param name="sql">The SQL query to union with the current query.</param>
    /// <param name="parameters">Optional SQL parameters used by the union query.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> UnionAllSql(string sql, object? parameters = null)
        => AddSetOperationSql("UNION ALL", sql, parameters);

    /// <summary>
    /// Adds an INTERSECT set operation using another expression-built query.
    /// </summary>
    /// <param name="configure">The callback that configures the second query.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Intersect(Action<IForgeAstSelectBuilder<T>> configure)
        => AddSetOperation("INTERSECT", configure);

    /// <summary>
    /// Adds an INTERSECT set operation using raw SQL.
    /// </summary>
    /// <param name="sql">The SQL query to intersect with the current query.</param>
    /// <param name="parameters">Optional SQL parameters used by the intersect query.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> IntersectSql(string sql, object? parameters = null)
        => AddSetOperationSql("INTERSECT", sql, parameters);

    /// <summary>
    /// Adds an EXCEPT set operation using another expression-built query.
    /// </summary>
    /// <param name="configure">The callback that configures the second query.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> Except(Action<IForgeAstSelectBuilder<T>> configure)
        => AddSetOperation("EXCEPT", configure);

    /// <summary>
    /// Adds an EXCEPT set operation using raw SQL.
    /// </summary>
    /// <param name="sql">The SQL query to subtract from the current query.</param>
    /// <param name="parameters">Optional SQL parameters used by the except query.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> ExceptSql(string sql, object? parameters = null)
        => AddSetOperationSql("EXCEPT", sql, parameters);

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public IForgeAstSelectBuilder<T> OrderBy(Expression<Func<T, object>> column)
    {
        _orderBy = $"{ForgeAstExpression.MemberName(column)} ASC";
        return this;
    }

    /// <summary>
    /// Executes the OrderByDescending operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
    public IForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column)
    {
        _orderBy = $"{ForgeAstExpression.MemberName(column)} DESC";
        return this;
    }

    /// <summary>
    /// Executes the OrderBySql operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBySql operation.</returns>
    public IForgeAstSelectBuilder<T> OrderBySql(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    /// <summary>
    /// Executes the Skip operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Skip operation.</returns>
    public IForgeAstSelectBuilder<T> Skip(int rows)
    {
        _skip = rows;
        return this;
    }

    /// <summary>
    /// Executes the Take operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Take operation.</returns>
    public IForgeAstSelectBuilder<T> Take(int rows)
    {
        _take = rows;
        return this;
    }

    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Render operation.</returns>
    public ForgeRenderedSql Render(IForgeDatabaseProvider provider)
    {
        _table ??= ResolveTableName(typeof(T));
        var sql = new StringBuilder();
        AppendCtes(sql);
        AppendSelect(sql);
        AppendFrom(sql);
        AppendJoins(sql);
        AppendWhere(sql);
        AppendGroupBy(sql);
        AppendHaving(sql);
        AppendOrderBy(sql);
        AppendPaging(sql, provider);
        AppendSetOperations(sql);
        return new ForgeRenderedSql(sql.ToString(), BuildFinalParameters());
    }

    /// <summary>
    /// Executes the RenderCount operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the RenderCount operation.</returns>
    public ForgeRenderedSql RenderCount(IForgeDatabaseProvider provider)
    {
        var rendered = RenderWithoutOrderAndPaging(provider);
        return new ForgeRenderedSql($"SELECT COUNT(1) FROM ({rendered.Sql}) ForgeCount", rendered.Parameters);
    }

    /// <summary>
    /// Executes the RenderAny operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the RenderAny operation.</returns>
    public ForgeRenderedSql RenderAny(IForgeDatabaseProvider provider)
    {
        var rendered = RenderWithoutOrderAndPaging(provider);
        return new ForgeRenderedSql($"SELECT CASE WHEN EXISTS ({rendered.Sql}) THEN 1 ELSE 0 END", rendered.Parameters);
    }

    /// <summary>
    /// Adds an IN condition against the default Id column.
    /// </summary>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="ids">The key values used by the IN condition.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> WhereIds<TKey>(IEnumerable<TKey> ids)
        => WhereIdsSql("Id", ids);

    /// <summary>
    /// Adds an IN condition against a key column selected by expression.
    /// </summary>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keySelector">The expression that selects the key column.</param>
    /// <param name="ids">The key values used by the IN condition.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> WhereIds<TKey>(Expression<Func<T, TKey>> keySelector, IEnumerable<TKey> ids)
        => WhereIdsSql(ForgeAstExpression.MemberName(ToObjectExpression(keySelector)), ids);

    /// <summary>
    /// Adds an IN condition against an explicit SQL key column.
    /// </summary>
    /// <typeparam name="TKey">The key value type.</typeparam>
    /// <param name="keyColumn">The SQL key column used by the IN condition.</param>
    /// <param name="ids">The key values used by the IN condition.</param>
    /// <returns>The current AST select builder.</returns>
    public IForgeAstSelectBuilder<T> WhereIdsSql<TKey>(string keyColumn, IEnumerable<TKey> ids)
    {
        var values = ids.Select(x => (object?)x).Where(x => x is not null).Distinct().ToList();
        if (values.Count == 0)
        {
            _where.Add("1 = 0");
            return this;
        }

        var parameterName = "ids" + _parameterIndex++;
        _parameters[parameterName] = values;
        _where.Add($"{keyColumn} IN @{parameterName}");
        return this;
    }

    /// <summary>
    /// Renders the current AST query as a DELETE command using the configured filters.
    /// </summary>
    /// <param name="provider">The database provider used to render provider-aware SQL.</param>
    /// <returns>The rendered DELETE SQL and parameters.</returns>
    public ForgeRenderedSql RenderDelete(IForgeDatabaseProvider provider)
    {
        _table ??= ResolveTableName(typeof(T));
        var sql = new StringBuilder();
        sql.Append("DELETE FROM ").Append(_table);
        AppendWhere(sql);
        return new ForgeRenderedSql(sql.ToString(), BuildFinalParameters());
    }

    /// <summary>
    /// Renders the current AST query as an UPDATE command using the configured filters.
    /// </summary>
    /// <param name="provider">The database provider used to render provider-aware SQL.</param>
    /// <param name="values">An object containing the columns and values to update.</param>
    /// <returns>The rendered UPDATE SQL and parameters.</returns>
    public ForgeRenderedSql RenderUpdate(IForgeDatabaseProvider provider, object values)
    {
        _table ??= ResolveTableName(typeof(T));
        var setParts = new List<string>();

        foreach (var property in values.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead))
        {
            var parameterName = "set_" + property.Name;
            setParts.Add($"{property.Name} = @{parameterName}");
            _parameters[parameterName] = property.GetValue(values);
        }

        if (setParts.Count == 0)
            throw new InvalidOperationException("At least one update value is required.");

        var sql = new StringBuilder();
        sql.Append("UPDATE ").Append(_table).Append(" SET ").Append(string.Join(", ", setParts));
        AppendWhere(sql);
        return new ForgeRenderedSql(sql.ToString(), BuildFinalParameters());
    }

    private static Expression<Func<T, object>> ToObjectExpression<TKey>(Expression<Func<T, TKey>> expression)
    {
        var converted = Expression.Convert(expression.Body, typeof(object));
        return Expression.Lambda<Func<T, object>>(converted, expression.Parameters);
    }

    private IForgeAstSelectBuilder<T> AddJoin(string joinSql)
    {
        _joins.Add(joinSql);
        return this;
    }

    private void AppendCtes(StringBuilder sql)
    {
        if (_ctes.Count == 0) return;
        sql.Append("WITH ");
        sql.Append(string.Join(", ", _ctes.Select(x => $"{x.Name} AS ({x.Sql})")));
        sql.AppendLine();
    }

    private void AppendSelect(StringBuilder sql)
    {
        sql.Append("SELECT ");
        if (_distinct) sql.Append("DISTINCT ");
        sql.Append(_columns.Count == 0 ? "*" : string.Join(", ", _columns));
    }

    private void AppendFrom(StringBuilder sql)
    {
        sql.Append(" FROM ");
        sql.Append(_table);
        if (!string.IsNullOrWhiteSpace(_temporalClause))
            sql.Append(' ').Append(_temporalClause);
        if (!string.IsNullOrWhiteSpace(_alias))
            sql.Append(' ').Append(_alias);
    }

    private void AppendJoins(StringBuilder sql)
    {
        if (_joins.Count > 0)
            sql.Append(' ').Append(string.Join(" ", _joins));
    }

    private void AppendWhere(StringBuilder sql)
    {
        if (_where.Count > 0)
            sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
    }

    private void AppendGroupBy(StringBuilder sql)
    {
        if (_groupBy.Count > 0)
            sql.Append(" GROUP BY ").Append(string.Join(", ", _groupBy));
    }

    private void AppendHaving(StringBuilder sql)
    {
        if (!string.IsNullOrWhiteSpace(_having))
            sql.Append(" HAVING ").Append(_having);
    }

    private void AppendOrderBy(StringBuilder sql)
    {
        if (!string.IsNullOrWhiteSpace(_orderBy))
            sql.Append(" ORDER BY ").Append(_orderBy);
    }

    private void AppendPaging(StringBuilder sql, IForgeDatabaseProvider provider)
    {
        if (!_take.HasValue) return;
        if (provider.ProviderName.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) ||
            provider.ProviderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_orderBy))
                sql.Append(" ORDER BY 1");
            sql.Append($" OFFSET {_skip ?? 0} ROWS FETCH NEXT {_take.Value} ROWS ONLY");
            return;
        }
        sql.Append($" LIMIT {_take.Value} OFFSET {_skip ?? 0}");
    }

    private object? BuildFinalParameters()
    {
        if (_rawParameters is not null && _parameters.Count == 0)
            return _rawParameters;
        if (_rawParameters is null)
            return _parameters;
        var merged = new Dictionary<string, object?>(_parameters, StringComparer.OrdinalIgnoreCase);
        foreach (var item in ParameterObjectReader.Read(_rawParameters))
            merged[item.Key] = item.Value;
        return merged;
    }

    private void MergeParameters(IReadOnlyDictionary<string, object?> parameters)
    {
        foreach (var parameter in parameters)
            _parameters[parameter.Key] = parameter.Value;
    }

    private void MergeRawParameters(object? parameters)
    {
        if (parameters is null) return;
        if (_rawParameters is null && _parameters.Count == 0)
        {
            _rawParameters = parameters;
            return;
        }

        foreach (var item in ParameterObjectReader.Read(parameters))
            _parameters[item.Key] = item.Value;
    }

    private ForgeRenderedSql RenderWithoutOrderAndPaging(IForgeDatabaseProvider provider)
    {
        _table ??= ResolveTableName(typeof(T));
        var sql = new StringBuilder();
        AppendCtes(sql);
        AppendSelect(sql);
        AppendFrom(sql);
        AppendJoins(sql);
        AppendWhere(sql);
        AppendGroupBy(sql);
        AppendHaving(sql);
        return new ForgeRenderedSql(sql.ToString(), BuildFinalParameters());
    }

    private static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(ForgeTableAttribute), false).Cast<ForgeTableAttribute>().FirstOrDefault();
        return attr?.Name ?? type.Name;
    }
    /// <summary>
    /// Executes the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    public IForgeAstSelectBuilder<T> Join<TJoin>(
    Expression<Func<T, TJoin, bool>> on)
    {
        return InnerJoin(on);
    }

    /// <summary>
    /// Executes the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    public IForgeAstSelectBuilder<T> InnerJoin<TJoin>(
        Expression<Func<T, TJoin, bool>> on)
    {
        return AddTypedJoin<TJoin>("INNER JOIN", on);
    }

    /// <summary>
    /// Executes the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    public IForgeAstSelectBuilder<T> LeftJoin<TJoin>(
        Expression<Func<T, TJoin, bool>> on)
    {
        return AddTypedJoin<TJoin>("LEFT JOIN", on);
    }

    /// <summary>
    /// Executes the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    public IForgeAstSelectBuilder<T> RightJoin<TJoin>(
        Expression<Func<T, TJoin, bool>> on)
    {
        return AddTypedJoin<TJoin>("RIGHT JOIN", on);
    }

    /// <summary>
    /// Executes the TJoin operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the TJoin operation.</returns>
    public IForgeAstSelectBuilder<T> FullJoin<TJoin>(
        Expression<Func<T, TJoin, bool>> on)
    {
        return AddTypedJoin<TJoin>("FULL OUTER JOIN", on);
    }

    private IForgeAstSelectBuilder<T> AddTypedJoin<TJoin>(
        string joinType,
        Expression<Func<T, TJoin, bool>> on)
    {
        var table = ResolveTableName(typeof(TJoin));
        var rightAlias = on.Parameters.Count > 1 && !string.IsNullOrWhiteSpace(on.Parameters[1].Name)
            ? on.Parameters[1].Name!
            : typeof(TJoin).Name[..1].ToLowerInvariant();
        var joinCondition = ForgeJoinExpression.Translate<T, TJoin>(on);

        _joins.Add($"{joinType} {table} {rightAlias} ON {joinCondition}");

        return this;
    }
    private IForgeAstSelectBuilder<T> AddSetOperation(string operatorName, Action<IForgeAstSelectBuilder<T>> configure)
    {
        var builder = new ForgeAstSelectBuilder<T>();
        builder._parameterIndex = _parameterIndex;
        configure(builder);
        var rendered = builder.Render(new PassthroughForgeDatabaseProvider());
        _parameterIndex = builder._parameterIndex;
        return AddSetOperationSql(operatorName, rendered.Sql, rendered.Parameters);
    }

    private IForgeAstSelectBuilder<T> AddSetOperationSql(string operatorName, string sql, object? parameters)
    {
        _setOperations.Add(new ForgeSetOperation(operatorName, sql, parameters));
        MergeSetOperationParameters(parameters);
        return this;
    }

    private void AppendSetOperations(StringBuilder sql)
    {
        foreach (var operation in _setOperations)
            sql.Append(' ').Append(operation.OperatorName).Append(' ').Append(operation.Sql);
    }

    private void MergeSetOperationParameters(object? parameters)
    {
        if (parameters is null) return;
        foreach (var item in ParameterObjectReader.Read(parameters))
            _parameters[item.Key] = item.Value;
    }

    private sealed record ForgeSetOperation(string OperatorName, string Sql, object? Parameters);

    private sealed class PassthroughForgeDatabaseProvider : IForgeDatabaseProvider
    {
        public string ProviderName => "Passthrough";
        public ForgeSqlDialect Dialect => new ForgeSqlDialect { Name = "SqlServer", ParameterPrefix = "@", OpenIdentifier = "[", CloseIdentifier = "]" };
        public ForgeProviderCapabilities Capabilities => new ForgeProviderCapabilities();

        public System.Data.Common.DbConnection CreateConnection(string connectionString)
            => throw new NotSupportedException("The pass-through provider is only used to render nested AST queries.");

        public ForgeCommand BuildGetById(ForgeEntityMetadata entity, object id) => throw new NotSupportedException();
        public ForgeCommand BuildGetByCode(ForgeEntityMetadata entity, string code) => throw new NotSupportedException();
        public ForgeCommand BuildGetByIds(ForgeEntityMetadata entity, IReadOnlyCollection<int> ids) => throw new NotSupportedException();
        public ForgeCommand BuildInsert(ForgeEntityMetadata entity, object entityInstance) => throw new NotSupportedException();
        public ForgeCommand BuildUpdate(ForgeEntityMetadata entity, object entityInstance) => throw new NotSupportedException();
        public ForgeCommand BuildDelete(ForgeEntityMetadata entity, object id) => throw new NotSupportedException();
        public ForgeCommand BuildPage(ForgePageRequest request) => throw new NotSupportedException();
        public ForgeCommand BuildCount(string baseSql, object? parameters = null) => throw new NotSupportedException();
        public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids) => throw new NotSupportedException();
        public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null) => throw new NotSupportedException();
        public ValueTask<int> BulkInsertAsync<TBulk>(System.Data.Common.DbConnection connection, string tableName, IReadOnlyCollection<TBulk> rows, CancellationToken cancellationToken = default) => ValueTask.FromResult(0);
        public ValueTask<int> BulkUpdateAsync<TBulk>(System.Data.Common.DbConnection connection, string tableName, IReadOnlyCollection<TBulk> rows, string keyColumn, CancellationToken cancellationToken = default) => ValueTask.FromResult(0);
        public ValueTask<int> BulkMergeAsync<TBulk>(System.Data.Common.DbConnection connection, string tableName, IReadOnlyCollection<TBulk> rows, string keyColumn, CancellationToken cancellationToken = default) => ValueTask.FromResult(0);
    }

    internal static class ParameterObjectReader
    {
        /// <summary>
        /// Executes the Read operation.
        /// </summary>
        /// <param name="parameters">The parameters value.</param>
        /// <returns>The result of the Read operation.</returns>
        public static IReadOnlyDictionary<string, object?> Read(object parameters)
        {
            if (parameters is IReadOnlyDictionary<string, object?> readonlyDictionary)
                return readonlyDictionary;
            if (parameters is IDictionary<string, object?> dictionary)
                return new Dictionary<string, object?>(dictionary, StringComparer.OrdinalIgnoreCase);
            return parameters.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanRead)
                .ToDictionary(x => x.Name, x => x.GetValue(parameters), StringComparer.OrdinalIgnoreCase);
        }
    }
}
