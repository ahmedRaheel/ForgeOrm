using ForgeORM.Abstractions;
using System.Text;

namespace ForgeORM.QueryAst;

public sealed class ForgeDynamicQueryBuilder : IForgeDynamicQueryBuilder
{
    public IForgeDynamicSelectBuilder Select(params string[] columns) => new ForgeDynamicSelectBuilder(columns.Length == 0 ? ["*"] : columns);
    public IForgeDynamicSelectBuilder SelectAll() => new ForgeDynamicSelectBuilder(["*"]);
}

internal sealed class ForgeDynamicSelectBuilder : IForgeDynamicSelectBuilder
{
    private readonly List<string> _columns;
    private readonly List<string> _joins = [];
    private readonly List<string> _where = [];
    private readonly List<string> _groupBy = [];
    private readonly List<ForgeCte> _ctes = [];
    private string? _table;
    private string? _orderBy;
    private string? _having;
    private bool _distinct;
    private int? _skip;
    private int? _take;
    private object? _parameters;

    public ForgeDynamicSelectBuilder(IEnumerable<string> columns) => _columns = columns.ToList();
    public IForgeDynamicSelectBuilder Distinct() { _distinct = true; return this; }
    public IForgeDynamicSelectBuilder From(string table) { _table = table; return this; }
    public IForgeDynamicSelectBuilder Where(string condition, object? parameters = null) { _where.Add(condition); _parameters = parameters ?? _parameters; return this; }
    public IForgeDynamicSelectBuilder And(string condition, object? parameters = null) => Where(condition, parameters);
    public IForgeDynamicSelectBuilder Or(string condition, object? parameters = null) { if (_where.Count == 0) _where.Add(condition); else _where[^1] = $"({_where[^1]}) OR ({condition})"; _parameters = parameters ?? _parameters; return this; }
    public IForgeDynamicSelectBuilder Join(string table, string on) => InnerJoin(table, on);
    public IForgeDynamicSelectBuilder InnerJoin(string table, string on) { _joins.Add($"INNER JOIN {table} ON {on}"); return this; }
    public IForgeDynamicSelectBuilder LeftJoin(string table, string on) { _joins.Add($"LEFT JOIN {table} ON {on}"); return this; }
    public IForgeDynamicSelectBuilder RightJoin(string table, string on) { _joins.Add($"RIGHT JOIN {table} ON {on}"); return this; }
    public IForgeDynamicSelectBuilder FullJoin(string table, string on) { _joins.Add($"FULL OUTER JOIN {table} ON {on}"); return this; }
    public IForgeDynamicSelectBuilder CrossJoin(string table) { _joins.Add($"CROSS JOIN {table}"); return this; }
    public IForgeDynamicSelectBuilder CrossApply(string tableExpression, string alias) { _joins.Add($"CROSS APPLY ({tableExpression}) {alias}"); return this; }
    public IForgeDynamicSelectBuilder OuterApply(string tableExpression, string alias) { _joins.Add($"OUTER APPLY ({tableExpression}) {alias}"); return this; }
    public IForgeDynamicSelectBuilder WithCte(string name, string sql) { _ctes.Add(new ForgeCte(name, sql)); return this; }
    public IForgeDynamicSelectBuilder GroupBy(params string[] columns) { _groupBy.AddRange(columns); return this; }
    public IForgeDynamicSelectBuilder Having(string condition) { _having = condition; return this; }
    public IForgeDynamicSelectBuilder OrderBy(string orderBy) { _orderBy = orderBy; return this; }
    public IForgeDynamicSelectBuilder Skip(int rows) { _skip = rows; return this; }
    public IForgeDynamicSelectBuilder Take(int rows) { _take = rows; return this; }
    public ForgeRenderedSql Build(IForgeDatabaseProvider provider) => Render(provider);

    public ForgeRenderedSql Render(IForgeDatabaseProvider provider)
    {
        if (string.IsNullOrWhiteSpace(_table)) throw new InvalidOperationException("FROM table is required.");

        var sql = new StringBuilder();
        if (_ctes.Count > 0)
        {
            sql.Append("WITH ");
            sql.Append(string.Join(", ", _ctes.Select(x => $"{x.Name} AS ({x.Sql})")));
            sql.AppendLine();
        }

        sql.Append("SELECT ");
        if (_distinct) sql.Append("DISTINCT ");
        sql.Append(string.Join(", ", _columns));
        sql.Append(" FROM ").Append(_table);
        if (_joins.Count > 0) sql.Append(' ').Append(string.Join(" ", _joins));
        if (_where.Count > 0) sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        if (_groupBy.Count > 0) sql.Append(" GROUP BY ").Append(string.Join(", ", _groupBy));
        if (!string.IsNullOrWhiteSpace(_having)) sql.Append(" HAVING ").Append(_having);
        if (!string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY ").Append(_orderBy);
        if (_take.HasValue)
        {
            if (provider.ProviderName.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) || provider.ProviderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY 1");
                sql.Append($" OFFSET {_skip ?? 0} ROWS FETCH NEXT {_take.Value} ROWS ONLY");
            }
            else
            {
                sql.Append($" LIMIT {_take.Value} OFFSET {_skip ?? 0}");
            }
        }
        return new ForgeRenderedSql(sql.ToString(), _parameters);
    }
}
