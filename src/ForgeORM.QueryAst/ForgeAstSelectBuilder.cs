using ForgeORM.Abstractions;
using System.Linq.Expressions;
using System.Text;

namespace ForgeORM.QueryAst;

internal sealed class ForgeAstSelectBuilder<T> : IForgeAstSelectBuilder<T>
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

    public IForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns)
    {
        _columns.AddRange(columns.Select(ForgeAstExpression.MemberName));
        return this;
    }

    public IForgeAstSelectBuilder<T> Columns(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    public IForgeAstSelectBuilder<T> Distinct()
    {
        _distinct = true;
        return this;
    }

    public IForgeAstSelectBuilder<T> From(string? tableName = null)
    {
        _table = tableName ?? ResolveTableName(typeof(T));
        return this;
    }

    public IForgeAstSelectBuilder<T> As(string alias)
    {
        _alias = alias;
        return this;
    }

    public IForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        var result = ForgeAstExpression.Translate(predicate, _parameterIndex);
        _parameterIndex += result.Parameters.Count;
        MergeParameters(result.Parameters);
        _where.Add(result.Sql);
        return this;
    }

    public IForgeAstSelectBuilder<T> WhereSql(string condition, object? parameters = null)
    {
        _where.Add(condition);
        if (parameters is not null) _rawParameters = parameters;
        return this;
    }

    public IForgeAstSelectBuilder<T> And(Expression<Func<T, bool>> predicate) => Where(predicate);
    public IForgeAstSelectBuilder<T> AndSql(string condition, object? parameters = null) => WhereSql(condition, parameters);

    public IForgeAstSelectBuilder<T> Or(Expression<Func<T, bool>> predicate)
    {
        var result = ForgeAstExpression.Translate(predicate, _parameterIndex);
        _parameterIndex += result.Parameters.Count;
        MergeParameters(result.Parameters);

        if (_where.Count == 0) _where.Add(result.Sql);
        else _where[^1] = $"({_where[^1]}) OR ({result.Sql})";

        return this;
    }

    public IForgeAstSelectBuilder<T> OrSql(string condition, object? parameters = null)
    {
        if (_where.Count == 0) _where.Add(condition);
        else _where[^1] = $"({_where[^1]}) OR ({condition})";
        if (parameters is not null) _rawParameters = parameters;
        return this;
    }

    public IForgeAstSelectBuilder<T> Join(string table, string on) => InnerJoin(table, on);
    public IForgeAstSelectBuilder<T> InnerJoin(string table, string on) => AddJoin($"INNER JOIN {table} ON {on}");
    public IForgeAstSelectBuilder<T> LeftJoin(string table, string on) => AddJoin($"LEFT JOIN {table} ON {on}");
    public IForgeAstSelectBuilder<T> RightJoin(string table, string on) => AddJoin($"RIGHT JOIN {table} ON {on}");
    public IForgeAstSelectBuilder<T> FullJoin(string table, string on) => AddJoin($"FULL OUTER JOIN {table} ON {on}");
    public IForgeAstSelectBuilder<T> CrossJoin(string table) => AddJoin($"CROSS JOIN {table}");
    public IForgeAstSelectBuilder<T> CrossApply(string tableExpression, string alias) => AddJoin($"CROSS APPLY ({tableExpression}) {alias}");
    public IForgeAstSelectBuilder<T> OuterApply(string tableExpression, string alias) => AddJoin($"OUTER APPLY ({tableExpression}) {alias}");

    public IForgeAstSelectBuilder<T> WithCte(string name, string sql)
    {
        _ctes.Add(new ForgeCte(name, sql));
        return this;
    }

    public IForgeAstSelectBuilder<T> WithCte(ForgeCte cte)
    {
        _ctes.Add(cte);
        return this;
    }

    public IForgeAstSelectBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns)
    {
        _groupBy.AddRange(columns.Select(ForgeAstExpression.MemberName));
        return this;
    }

    public IForgeAstSelectBuilder<T> GroupBy(params string[] columns)
    {
        _groupBy.AddRange(columns);
        return this;
    }

    public IForgeAstSelectBuilder<T> HavingSql(string condition)
    {
        _having = condition;
        return this;
    }

    public IForgeAstSelectBuilder<T> OrderBy(Expression<Func<T, object>> column)
    {
        _orderBy = $"{ForgeAstExpression.MemberName(column)} ASC";
        return this;
    }

    public IForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column)
    {
        _orderBy = $"{ForgeAstExpression.MemberName(column)} DESC";
        return this;
    }

    public IForgeAstSelectBuilder<T> OrderBySql(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    public IForgeAstSelectBuilder<T> Skip(int rows)
    {
        _skip = rows;
        return this;
    }

    public IForgeAstSelectBuilder<T> Take(int rows)
    {
        _take = rows;
        return this;
    }

    public ForgeRenderedSql Render(IForgeDatabaseProvider provider)
    {
        _table ??= ResolveTableName(typeof(T));

        var sql = new StringBuilder();

        if (_ctes.Count > 0)
        {
            sql.Append("WITH ");
            sql.Append(string.Join(", ", _ctes.Select(x => $"{x.Name} AS ({x.Sql})")));
            sql.AppendLine();
        }

        sql.Append("SELECT ");
        if (_distinct) sql.Append("DISTINCT ");
        sql.Append(_columns.Count == 0 ? "*" : string.Join(", ", _columns));
        sql.Append(" FROM ");
        sql.Append(_table);
        if (!string.IsNullOrWhiteSpace(_alias)) sql.Append(' ').Append(_alias);

        if (_joins.Count > 0) sql.Append(' ').Append(string.Join(" ", _joins));
        if (_where.Count > 0) sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        if (_groupBy.Count > 0) sql.Append(" GROUP BY ").Append(string.Join(", ", _groupBy));
        if (!string.IsNullOrWhiteSpace(_having)) sql.Append(" HAVING ").Append(_having);
        if (!string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY ").Append(_orderBy);
        if (_take.HasValue) AppendPaging(sql, provider);

        return new ForgeRenderedSql(sql.ToString(), BuildParameterObject());
    }

    private IForgeAstSelectBuilder<T> AddJoin(string joinSql)
    {
        _joins.Add(joinSql);
        return this;
    }

    private void AppendPaging(StringBuilder sql, IForgeDatabaseProvider provider)
    {
        if (provider.ProviderName.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) ||
            provider.ProviderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY 1");
            sql.Append($" OFFSET {_skip ?? 0} ROWS FETCH NEXT {_take!.Value} ROWS ONLY");
            return;
        }

        sql.Append($" LIMIT {_take!.Value} OFFSET {_skip ?? 0}");
    }

    private void MergeParameters(Dictionary<string, object?> values)
    {
        foreach (var item in values) _parameters[item.Key] = item.Value;
    }

    private object? BuildParameterObject()
    {
        if (_rawParameters is null && _parameters.Count == 0) return null;
        if (_rawParameters is not null && _parameters.Count == 0) return _rawParameters;
        return _parameters;
    }

    private static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(ForgeTableAttribute), false).Cast<ForgeTableAttribute>().FirstOrDefault();
        return attr?.Name ?? type.Name;
    }
}
