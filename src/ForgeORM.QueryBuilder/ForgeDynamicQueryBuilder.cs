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
    /// <summary>
    /// Initializes or executes the Select operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder Select(params string[] columns) => new ForgeSelectBuilder(columns.Length == 0 ? ["*"] : columns);
    /// <summary>
    /// Initializes or executes the SelectAll operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Initializes or executes the ForgeSelectBuilder operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    public ForgeSelectBuilder(IEnumerable<string> columns) => _columns = columns.ToList();
    /// <summary>
    /// Initializes or executes the From operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder From(string table) { _table = table; return this; }
    /// <summary>
    /// Initializes or executes the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder Where(string condition, object? parameters = null) { _where.Add(condition); if (parameters != null) _parameters.Add(parameters); return this; }
    /// <summary>
    /// Initializes or executes the And operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder And(string condition, object? parameters = null) => Where(condition, parameters);
    /// <summary>
    /// Initializes or executes the Or operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder Or(string condition, object? parameters = null) { if (_where.Count == 0) _where.Add(condition); else _where[^1] = $"({_where[^1]}) OR ({condition})"; if (parameters != null) _parameters.Add(parameters); return this; }
    /// <summary>
    /// Initializes or executes the Join operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder Join(string table, string on) { _joins.Add($"INNER JOIN {table} ON {on}"); return this; }
    /// <summary>
    /// Initializes or executes the LeftJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder LeftJoin(string table, string on) { _joins.Add($"LEFT JOIN {table} ON {on}"); return this; }
    /// <summary>
    /// Initializes or executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder OrderBy(string orderBy) { _orderBy = orderBy; return this; }
    /// <summary>
    /// Initializes or executes the GroupBy operation.
    /// </summary>
    /// <param name="_groupBy">The _groupBy value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder GroupBy(params string[] columns) { _groupBy.AddRange(columns); return this; }
    /// <summary>
    /// Initializes or executes the Having operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder Having(string condition) { _having = condition; return this; }
    /// <summary>
    /// Initializes or executes the Skip operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder Skip(int rows) { _skip = rows; return this; }
    /// <summary>
    /// Initializes or executes the Take operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The operation result.</returns>
    public IForgeSelectBuilder Take(int rows) { _take = rows; return this; }

    /// <summary>
    /// Initializes or executes the Build operation.
    /// </summary>
    /// <returns>The operation result.</returns>
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
