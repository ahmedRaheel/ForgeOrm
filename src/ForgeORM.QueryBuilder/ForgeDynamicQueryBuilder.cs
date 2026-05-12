using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryBuilder;

public interface IForgeSelectQueryBuilder
{
    IForgeSelectBuilder Select(params string[] columns);
    IForgeSelectBuilder SelectAll();
}

public interface IForgeSelectBuilder
{
    IForgeSelectBuilder From(string table);
    IForgeSelectBuilder Where(string condition, object? parameters = null);
    IForgeSelectBuilder And(string condition, object? parameters = null);
    IForgeSelectBuilder Or(string condition, object? parameters = null);
    IForgeSelectBuilder Join(string table, string on);
    IForgeSelectBuilder LeftJoin(string table, string on);
    IForgeSelectBuilder OrderBy(string orderBy);
    IForgeSelectBuilder GroupBy(params string[] columns);
    IForgeSelectBuilder Having(string condition);
    IForgeSelectBuilder Skip(int rows);
    IForgeSelectBuilder Take(int rows);
    ForgeBuiltQuery Build();
}

public sealed class ForgeDynamicQueryBuilder : IForgeSelectQueryBuilder
{
    public IForgeSelectBuilder Select(params string[] columns) => new ForgeSelectBuilder(columns.Length == 0 ? ["*"] : columns);
    public IForgeSelectBuilder SelectAll() => new ForgeSelectBuilder(["*"]);
}

internal sealed class ForgeSelectBuilder : IForgeSelectBuilder
{
    private readonly List<string> _columns;
    private readonly List<string> _joins = [];
    private readonly List<string> _where = [];
    private readonly List<object?> _parameters = [];
    private readonly List<string> _groupBy = [];
    private string? _table;
    private string? _orderBy;
    private string? _having;
    private int? _skip;
    private int? _take;

    public ForgeSelectBuilder(IEnumerable<string> columns) => _columns = columns.ToList();
    public IForgeSelectBuilder From(string table) { _table = table; return this; }
    public IForgeSelectBuilder Where(string condition, object? parameters = null) { _where.Add(condition); if (parameters != null) _parameters.Add(parameters); return this; }
    public IForgeSelectBuilder And(string condition, object? parameters = null) => Where(condition, parameters);
    public IForgeSelectBuilder Or(string condition, object? parameters = null) { if (_where.Count == 0) _where.Add(condition); else _where[^1] = $"({_where[^1]}) OR ({condition})"; if (parameters != null) _parameters.Add(parameters); return this; }
    public IForgeSelectBuilder Join(string table, string on) { _joins.Add($"INNER JOIN {table} ON {on}"); return this; }
    public IForgeSelectBuilder LeftJoin(string table, string on) { _joins.Add($"LEFT JOIN {table} ON {on}"); return this; }
    public IForgeSelectBuilder OrderBy(string orderBy) { _orderBy = orderBy; return this; }
    public IForgeSelectBuilder GroupBy(params string[] columns) { _groupBy.AddRange(columns); return this; }
    public IForgeSelectBuilder Having(string condition) { _having = condition; return this; }
    public IForgeSelectBuilder Skip(int rows) { _skip = rows; return this; }
    public IForgeSelectBuilder Take(int rows) { _take = rows; return this; }

    public ForgeBuiltQuery Build()
    {
        if (string.IsNullOrWhiteSpace(_table)) throw new InvalidOperationException("FROM table is required.");
        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(string.Join(", ", _columns)).Append(" FROM ").Append(_table);
        if (_joins.Count > 0) sql.Append(' ').Append(string.Join(" ", _joins));
        if (_where.Count > 0) sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        if (_groupBy.Count > 0) sql.Append(" GROUP BY ").Append(string.Join(", ", _groupBy));
        if (!string.IsNullOrWhiteSpace(_having)) sql.Append(" HAVING ").Append(_having);
        if (!string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY ").Append(_orderBy);
        if (_take.HasValue) sql.Append($" OFFSET {_skip ?? 0} ROWS FETCH NEXT {_take.Value} ROWS ONLY");
        return new ForgeBuiltQuery { Sql = sql.ToString(), Parameters = _parameters.Count == 1 ? _parameters[0] : _parameters };
    }
}
