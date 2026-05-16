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
