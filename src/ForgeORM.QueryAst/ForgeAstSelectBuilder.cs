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
        if (parameters is not null)
            _rawParameters = parameters;
        return this;
    }

    public IForgeAstSelectBuilder<T> And(Expression<Func<T, bool>> predicate) => Where(predicate);
    public IForgeAstSelectBuilder<T> AndSql(string condition, object? parameters = null) => WhereSql(condition, parameters);

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

    public IForgeAstSelectBuilder<T> OrSql(string condition, object? parameters = null)
    {
        if (_where.Count == 0)
            _where.Add(condition);
        else
            _where[^1] = $"({_where[^1]}) OR ({condition})";

        if (parameters is not null)
            _rawParameters = parameters;

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

    private static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(ForgeTableAttribute), false).Cast<ForgeTableAttribute>().FirstOrDefault();
        return attr?.Name ?? type.Name;
    }
    public IForgeAstSelectBuilder<T> Join<TJoin>(
    Expression<Func<T, TJoin, bool>> on)
    {
        return InnerJoin(on);
    }

    public IForgeAstSelectBuilder<T> InnerJoin<TJoin>(
        Expression<Func<T, TJoin, bool>> on)
    {
        return AddTypedJoin<TJoin>("INNER JOIN", on);
    }

    public IForgeAstSelectBuilder<T> LeftJoin<TJoin>(
        Expression<Func<T, TJoin, bool>> on)
    {
        return AddTypedJoin<TJoin>("LEFT JOIN", on);
    }

    public IForgeAstSelectBuilder<T> RightJoin<TJoin>(
        Expression<Func<T, TJoin, bool>> on)
    {
        return AddTypedJoin<TJoin>("RIGHT JOIN", on);
    }

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
