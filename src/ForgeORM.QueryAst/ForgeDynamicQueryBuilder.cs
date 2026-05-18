using ForgeORM.Abstractions;
using System.Text;

namespace ForgeORM.QueryAst;

public sealed class ForgeDynamicQueryBuilder : IForgeDynamicQueryBuilder
{
    /// <summary>
    /// Executes the Select operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the Select operation.</returns>
    public IForgeDynamicSelectBuilder Select(params string[] columns) => new ForgeDynamicSelectBuilder(columns.Length == 0 ? ["*"] : columns);
    /// <summary>
    /// Executes the SelectAll operation.
    /// </summary>
    /// <returns>The result of the SelectAll operation.</returns>
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

    /// <summary>
    /// Executes the ForgeDynamicSelectBuilder operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The result of the ForgeDynamicSelectBuilder operation.</returns>
    public ForgeDynamicSelectBuilder(IEnumerable<string> columns) => _columns = columns.ToList();
    /// <summary>
    /// Executes the Distinct operation.
    /// </summary>
    /// <returns>The result of the Distinct operation.</returns>
    public IForgeDynamicSelectBuilder Distinct() { _distinct = true; return this; }
    /// <summary>
    /// Executes the From operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the From operation.</returns>
    public IForgeDynamicSelectBuilder From(string table) { _table = table; return this; }
    /// <summary>
    /// Executes the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Where operation.</returns>
    public IForgeDynamicSelectBuilder Where(string condition, object? parameters = null) { _where.Add(condition); _parameters = parameters ?? _parameters; return this; }
    /// <summary>
    /// Executes the And operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the And operation.</returns>
    public IForgeDynamicSelectBuilder And(string condition, object? parameters = null) => Where(condition, parameters);
    /// <summary>
    /// Executes the Or operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Or operation.</returns>
    public IForgeDynamicSelectBuilder Or(string condition, object? parameters = null) { if (_where.Count == 0) _where.Add(condition); else _where[^1] = $"({_where[^1]}) OR ({condition})"; _parameters = parameters ?? _parameters; return this; }
    /// <summary>
    /// Executes the Join operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the Join operation.</returns>
    public IForgeDynamicSelectBuilder Join(string table, string on) => InnerJoin(table, on);
    /// <summary>
    /// Executes the InnerJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the InnerJoin operation.</returns>
    public IForgeDynamicSelectBuilder InnerJoin(string table, string on) { _joins.Add($"INNER JOIN {table} ON {on}"); return this; }
    /// <summary>
    /// Executes the LeftJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the LeftJoin operation.</returns>
    public IForgeDynamicSelectBuilder LeftJoin(string table, string on) { _joins.Add($"LEFT JOIN {table} ON {on}"); return this; }
    /// <summary>
    /// Executes the RightJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the RightJoin operation.</returns>
    public IForgeDynamicSelectBuilder RightJoin(string table, string on) { _joins.Add($"RIGHT JOIN {table} ON {on}"); return this; }
    /// <summary>
    /// Executes the FullJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the FullJoin operation.</returns>
    public IForgeDynamicSelectBuilder FullJoin(string table, string on) { _joins.Add($"FULL OUTER JOIN {table} ON {on}"); return this; }
    /// <summary>
    /// Executes the CrossJoin operation.
    /// </summary>
    /// <returns>The result of the CrossJoin operation.</returns>
    public IForgeDynamicSelectBuilder CrossJoin(string table) { _joins.Add($"CROSS JOIN {table}"); return this; }
    /// <summary>
    /// Executes the CrossApply operation.
    /// </summary>
    /// <param name="tableExpression">The tableExpression value.</param>
    /// <returns>The result of the CrossApply operation.</returns>
    public IForgeDynamicSelectBuilder CrossApply(string tableExpression, string alias) { _joins.Add($"CROSS APPLY ({tableExpression}) {alias}"); return this; }
    /// <summary>
    /// Executes the OuterApply operation.
    /// </summary>
    /// <param name="tableExpression">The tableExpression value.</param>
    /// <returns>The result of the OuterApply operation.</returns>
    public IForgeDynamicSelectBuilder OuterApply(string tableExpression, string alias) { _joins.Add($"OUTER APPLY ({tableExpression}) {alias}"); return this; }
    /// <summary>
    /// Executes the WithCte operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WithCte operation.</returns>
    public IForgeDynamicSelectBuilder WithCte(string name, string sql) { _ctes.Add(new ForgeCte(name, sql)); return this; }
    /// <summary>
    /// Executes the GroupBy operation.
    /// </summary>
    /// <param name="_groupBy">The _groupBy value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    public IForgeDynamicSelectBuilder GroupBy(params string[] columns) { _groupBy.AddRange(columns); return this; }
    /// <summary>
    /// Executes the Having operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <returns>The result of the Having operation.</returns>
    public IForgeDynamicSelectBuilder Having(string condition) { _having = condition; return this; }
    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public IForgeDynamicSelectBuilder OrderBy(string orderBy) { _orderBy = orderBy; return this; }
    /// <summary>
    /// Executes the Skip operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Skip operation.</returns>
    public IForgeDynamicSelectBuilder Skip(int rows) { _skip = rows; return this; }
    /// <summary>
    /// Executes the Take operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Take operation.</returns>
    public IForgeDynamicSelectBuilder Take(int rows) { _take = rows; return this; }
    /// <summary>
    /// Executes the Build operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Build operation.</returns>
    public ForgeRenderedSql Build(IForgeDatabaseProvider provider) => Render(provider);

    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Render operation.</returns>
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
        if (_take.HasValue || _skip.HasValue)
        {
            var skip = Math.Max(0, _skip ?? 0);
            var take = _take.GetValueOrDefault();
            if (take <= 0) take = 1;
            if (skip == take) take++;

            if (provider.ProviderName.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) || provider.ProviderName.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY 1");
                sql.Append($" OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY");
            }
            else
            {
                sql.Append($" LIMIT {take} OFFSET {skip}");
            }
        }
        return new ForgeRenderedSql(sql.ToString(), _parameters);
    }
}
