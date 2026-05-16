using System.Linq.Expressions;

namespace ForgeORM.QueryBuilder;

public sealed class ForgeAdvancedQuery<T>
{
    private readonly List<string> _columns = [];
    private readonly List<string> _joins = [];
    private readonly List<string> _where = [];
    private readonly List<string> _groupBy = [];
    private readonly Dictionary<string, object?> _parameters = [];
    private string _from = typeof(T).Name;
    private string? _orderBy;
    private int? _skip;
    private int? _take;
    private int _parameterIndex;

    /// <summary>
    /// Initializes or executes the From operation.
    /// </summary>
    /// <param name="tableOrView">The tableOrView value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> From(string tableOrView)
    {
        _from = tableOrView;
        return this;
    }

    /// <summary>
    /// Initializes or executes the Select operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Initializes or executes the Select operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> Select(params Expression<Func<T, object>>[] columns)
    {
        _columns.AddRange(columns.Select(GetMemberName));
        return this;
    }

    /// <summary>
    /// Initializes or executes the LeftJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> LeftJoin(string table, string on)
    {
        _joins.Add($"LEFT JOIN {table} ON {on}");
        return this;
    }

    /// <summary>
    /// Initializes or executes the InnerJoin operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <param name="on">The on value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> InnerJoin(string table, string on)
    {
        _joins.Add($"INNER JOIN {table} ON {on}");
        return this;
    }

    /// <summary>
    /// Initializes or executes the Where operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> Where(string condition, object? parameters = null)
    {
        _where.Add(condition);
        if (parameters is not null)
            AddObjectParameters(parameters);
        return this;
    }

    /// <summary>
    /// Initializes or executes the WhereIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="where">The where value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> WhereIf(bool condition, string where, object? parameters = null)
        => condition ? Where(where, parameters) : this;

    /// <summary>
    /// Initializes or executes the Where operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        if (predicate.Body is not BinaryExpression binary)
            throw new NotSupportedException("ForgeAdvancedQuery currently supports simple binary predicates only.");

        var parameterName = "p" + _parameterIndex++;
        _where.Add($"{ReadMember(binary.Left)} {Operator(binary.NodeType)} @{parameterName}");
        _parameters[parameterName] = Expression.Lambda(binary.Right).Compile().DynamicInvoke();
        return this;
    }

    /// <summary>
    /// Initializes or executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> OrderBy(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    /// <summary>
    /// Initializes or executes the OrderByDescending operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> OrderByDescending(Expression<Func<T, object>> column)
    {
        _orderBy = $"{GetMemberName(column)} DESC";
        return this;
    }

    /// <summary>
    /// Initializes or executes the GroupBy operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> GroupBy(params string[] columns)
    {
        _groupBy.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Initializes or executes the Page operation.
    /// </summary>
    /// <param name="page">The page value.</param>
    /// <param name="pageSize">The pageSize value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedQuery<T> Page(int page, int pageSize)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;
        _skip = (page - 1) * pageSize;
        _take = pageSize;
        return this;
    }

    /// <summary>
    /// Initializes or executes the Build operation.
    /// </summary>
    /// <param name="provider">The provider value.</param>
    /// <returns>The operation result.</returns>
    public ForgeAdvancedRenderedQuery Build(string provider = "SqlServer")
    {
        var sql = $"SELECT {(_columns.Count == 0 ? "*" : string.Join(", ", _columns))} FROM {_from}";

        if (_joins.Count > 0)
            sql += " " + string.Join(" ", _joins);

        if (_where.Count > 0)
            sql += " WHERE " + string.Join(" AND ", _where);

        if (_groupBy.Count > 0)
            sql += " GROUP BY " + string.Join(", ", _groupBy);

        if (!string.IsNullOrWhiteSpace(_orderBy))
            sql += " ORDER BY " + _orderBy;

        if (_take.HasValue)
        {
            sql += provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
                ? $" OFFSET {_skip ?? 0} ROWS FETCH NEXT {_take.Value} ROWS ONLY"
                : $" LIMIT {_take.Value} OFFSET {_skip ?? 0}";
        }

        return new ForgeAdvancedRenderedQuery(sql, _parameters.Count == 0 ? null : _parameters);
    }

    private void AddObjectParameters(object parameters)
    {
        foreach (var prop in parameters.GetType().GetProperties())
            _parameters[prop.Name] = prop.GetValue(parameters);
    }

    private static string GetMemberName(Expression<Func<T, object>> expression)
    {
        return expression.Body switch
        {
            UnaryExpression unary when unary.Operand is MemberExpression member => member.Member.Name,
            MemberExpression member => member.Member.Name,
            _ => throw new NotSupportedException("Only member expressions are supported.")
        };
    }

    private static string ReadMember(Expression expression)
    {
        return expression switch
        {
            MemberExpression member => member.Member.Name,
            UnaryExpression unary when unary.Operand is MemberExpression member => member.Member.Name,
            _ => throw new NotSupportedException("Only member expressions are supported on the left side.")
        };
    }

    private static string Operator(ExpressionType type) => type switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "<>",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        _ => throw new NotSupportedException($"Operator {type} is not supported.")
    };
}

public sealed record ForgeAdvancedRenderedQuery(string Sql, object? Parameters);
