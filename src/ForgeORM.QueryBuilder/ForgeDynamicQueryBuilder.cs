using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.QueryBuilder;

public interface IForgeSelectQueryBuilder
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
    IForgeSelectBuilder Select(params string[] columns);
    /// <summary>
    /// Defines the SelectAll operation.
    /// </summary>
    /// <returns>The result of the SelectAll operation.</returns>
    IForgeSelectBuilder SelectAll();
}

public interface IForgeSelectBuilder
/// <summary>
/// Defines the From operation.
/// </summary>
/// <param name="table">The table value.</param>
/// <returns>The result of the From operation.</returns>
{
    /// <summary>
    /// Defines the From operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the From operation.</returns>
    IForgeSelectBuilder From(string table);
    /// <summary>
    /// Defines the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Where operation.</returns>
    IForgeSelectBuilder Where(string condition, object? parameters = null);
    /// <summary>
    /// Defines the And operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the And operation.</returns>
    IForgeSelectBuilder And(string condition, object? parameters = null);
    /// <summary>
    /// Defines the Or operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Or operation.</returns>
    IForgeSelectBuilder Or(string condition, object? parameters = null);
    /// <summary>
    /// Defines the Join operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the Join operation.</returns>
    IForgeSelectBuilder Join(string table, string on);
    /// <summary>
    /// Defines the LeftJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The result of the LeftJoin operation.</returns>
    IForgeSelectBuilder LeftJoin(string table, string on);
    /// <summary>
    /// Defines the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    IForgeSelectBuilder OrderBy(string orderBy);
    /// <summary>
    /// Defines the GroupBy operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    IForgeSelectBuilder GroupBy(params string[] columns);
    /// <summary>
    /// Defines the Having operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <returns>The result of the Having operation.</returns>
    IForgeSelectBuilder Having(string condition);
    /// <summary>
    /// Defines the Skip operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Skip operation.</returns>
    IForgeSelectBuilder Skip(int rows);
    /// <summary>
    /// Defines the Take operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Take operation.</returns>
    IForgeSelectBuilder Take(int rows);
    /// <summary>
    /// Defines the Build operation.
    /// </summary>
    /// <returns>The result of the Build operation.</returns>
    ForgeBuiltQuery Build();
}

public sealed class ForgeDynamicQueryBuilder : IForgeSelectQueryBuilder
{
    /// <summary>
    /// Executes the Select operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the Select operation.</returns>
    public IForgeSelectBuilder Select(params string[] columns) => new ForgeSelectBuilder(columns.Length == 0 ? ["*"] : columns);
    /// <summary>
    /// Executes the SelectAll operation.
    /// </summary>
    /// <returns>The result of the SelectAll operation.</returns>
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
    /// Executes the ForgeSelectBuilder operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The result of the ForgeSelectBuilder operation.</returns>
    public ForgeSelectBuilder(IEnumerable<string> columns) => _columns = columns.ToList();
    /// <summary>
    /// Executes the From operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the From operation.</returns>
    public IForgeSelectBuilder From(string table) { _table = table; return this; }
    /// <summary>
    /// Executes the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Where operation.</returns>
    public IForgeSelectBuilder Where(string condition, object? parameters = null) { _where.Add(condition); if (parameters != null) _parameters.Add(parameters); return this; }
    /// <summary>
    /// Executes the And operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the And operation.</returns>
    public IForgeSelectBuilder And(string condition, object? parameters = null) => Where(condition, parameters);
    /// <summary>
    /// Executes the Or operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Or operation.</returns>
    public IForgeSelectBuilder Or(string condition, object? parameters = null) { if (_where.Count == 0) _where.Add(condition); else _where[^1] = $"({_where[^1]}) OR ({condition})"; if (parameters != null) _parameters.Add(parameters); return this; }
    /// <summary>
    /// Executes the Join operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the Join operation.</returns>
    public IForgeSelectBuilder Join(string table, string on) { _joins.Add($"INNER JOIN {table} ON {on}"); return this; }
    /// <summary>
    /// Executes the LeftJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the LeftJoin operation.</returns>
    public IForgeSelectBuilder LeftJoin(string table, string on) { _joins.Add($"LEFT JOIN {table} ON {on}"); return this; }
    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public IForgeSelectBuilder OrderBy(string orderBy) { _orderBy = orderBy; return this; }
    /// <summary>
    /// Executes the GroupBy operation.
    /// </summary>
    /// <param name="_groupBy">The _groupBy value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    public IForgeSelectBuilder GroupBy(params string[] columns) { _groupBy.AddRange(columns); return this; }
    /// <summary>
    /// Executes the Having operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <returns>The result of the Having operation.</returns>
    public IForgeSelectBuilder Having(string condition) { _having = condition; return this; }
    /// <summary>
    /// Executes the Skip operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Skip operation.</returns>
    public IForgeSelectBuilder Skip(int rows) { _skip = rows; return this; }
    /// <summary>
    /// Executes the Take operation.
    /// </summary>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the Take operation.</returns>
    public IForgeSelectBuilder Take(int rows) { _take = rows; return this; }

    /// <summary>
    /// Executes the Build operation.
    /// </summary>
    /// <returns>The result of the Build operation.</returns>
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
