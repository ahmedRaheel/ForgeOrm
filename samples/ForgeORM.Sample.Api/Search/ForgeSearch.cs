using ForgeORM.Core;
using ForgeORM.QueryAst;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

public static class ForgeSearchExtensions
{
    /// <summary>
    /// Initializes or executes the Search operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeSearch<T> Search<T>(this ForgeDb db)
    {
        return new ForgeSearch<T>(db);
    }

    /// <summary>
    /// Initializes or executes the SearchProcedure operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="procedureName">The procedureName value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeProcedureSearch<T> SearchProcedure<T>(this ForgeDb db, string procedureName)
    {
        return new ForgeProcedureSearch<T>(db, procedureName);
    }
}

public sealed class ForgeSearch<T>
{
    private readonly ForgeDb _db;
    private readonly List<string> _columns = [];
    private readonly List<string> _where = [];
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private int _parameterIndex;
    private string? _fromSql;
    private string? _table;
    private string? _orderBy;
    private int? _page;
    private int? _pageSize;

    /// <summary>
    /// Initializes or executes the ForgeSearch operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    public ForgeSearch(ForgeDb db)
    {
        _db = db;
    }

    /// <summary>
    /// Initializes or executes the Select operation.
    /// </summary>
    /// <param name="columns">The columns value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Initializes or executes the From operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> From(string table)
    {
        _table = table;
        return this;
    }

    /// <summary>
    /// Initializes or executes the FromSql operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> FromSql(string sql)
    {
        _fromSql = sql;
        return this;
    }

    /// <summary>
    /// Initializes or executes the Where operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> Where(Expression<Func<T, bool>> predicate)
    {
        var translated = ForgeSearchExpression.Translate(predicate, _parameterIndex);
        _parameterIndex += translated.Parameters.Count;
        _where.Add(translated.Sql);
        Merge(translated.Parameters);
        return this;
    }

    /// <summary>
    /// Initializes or executes the WhereIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? Where(predicate) : this;
    }

    /// <summary>
    /// Initializes or executes the Where operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> Where(string sql, object? parameters = null)
    {
        _where.Add(sql);
        Merge(parameters);
        return this;
    }

    /// <summary>
    /// Initializes or executes the WhereIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> WhereIf(bool condition, string sql, object? parameters = null)
    {
        return condition ? Where(sql, parameters) : this;
    }

    /// <summary>
    /// Initializes or executes the Optional operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> Optional<TValue>(string column, TValue? value)
    {
        if (value is null)
            return this;

        var name = NextParameterName(column);
        _where.Add($"{column} = @{name}");
        _parameters[name] = value;
        return this;
    }

    /// <summary>
    /// Initializes or executes the OptionalLike operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> OptionalLike(string column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return this;

        var name = NextParameterName(column);
        _where.Add($"{column} LIKE @{name}");
        _parameters[name] = $"%{value}%";
        return this;
    }

    /// <summary>
    /// Initializes or executes the OptionalBetween operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="from">The from value.</param>
    /// <param name="to">The to value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> OptionalBetween<TValue>(string column, TValue? from, TValue? to)
    {
        if (from is not null)
        {
            var name = NextParameterName(column + "From");
            _where.Add($"{column} >= @{name}");
            _parameters[name] = from;
        }

        if (to is not null)
        {
            var name = NextParameterName(column + "To");
            _where.Add($"{column} <= @{name}");
            _parameters[name] = to;
        }

        return this;
    }

    /// <summary>
    /// Initializes or executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> OrderBy(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    /// <summary>
    /// Initializes or executes the Page operation.
    /// </summary>
    /// <param name="page">The page value.</param>
    /// <param name="pageSize">The pageSize value.</param>
    /// <returns>The operation result.</returns>
    public ForgeSearch<T> Page(int page, int pageSize)
    {
        _page = Math.Max(page, 1);
        _pageSize = Math.Max(pageSize, 1);
        return this;
    }

    /// <summary>
    /// Initializes or executes the Render operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public ForgeRenderedSql Render()
    {
        var sql = new StringBuilder();
        var baseSql = BuildBaseSql();

        sql.Append(baseSql);

        if (_where.Count > 0)
            sql.Append(" WHERE ").Append(string.Join(" AND ", _where));

        if (!string.IsNullOrWhiteSpace(_orderBy))
            sql.Append(" ORDER BY ").Append(_orderBy);

        if (_page.HasValue && _pageSize.HasValue)
        {
            if (string.IsNullOrWhiteSpace(_orderBy))
                sql.Append(" ORDER BY 1");

            var skip = (_page.Value - 1) * _pageSize.Value;
            sql.Append($" OFFSET {skip} ROWS FETCH NEXT {_pageSize.Value} ROWS ONLY");
        }

        return new ForgeRenderedSql(sql.ToString(), _parameters);
    }

    /// <summary>
    /// Initializes or executes the ToListAsync operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<T>> ToListAsync()
    {
        var query = Render();
        return await _db.QueryAsync<T>(query.Sql, query.Parameters);
    }

    /// <summary>
    /// Initializes or executes the ToPagedAsync operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public async Task<ForgePagedResult<T>> ToPagedAsync()
    {
        var dataQuery = Render();
        var countSql = BuildCountSql();
        var total = await _db.ExecuteScalarAsync<int>(countSql, _parameters);
        var items = await _db.QueryAsync<T>(dataQuery.Sql, dataQuery.Parameters);

        return new ForgePagedResult<T>
        {
            Items = items,
            Page = _page ?? 1,
            PageSize = _pageSize ?? items.Count,
            TotalRecords = total
        };
    }

    private string BuildBaseSql()
    {
        if (!string.IsNullOrWhiteSpace(_fromSql))
            return _fromSql.Trim();

        var table = string.IsNullOrWhiteSpace(_table) ? typeof(T).Name : _table;
        var columns = _columns.Count == 0 ? "*" : string.Join(", ", _columns);
        return $"SELECT {columns} FROM {table}";
    }

    private string BuildCountSql()
    {
        var baseSql = BuildBaseSql();
        var whereSql = _where.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", _where);
        return $"SELECT COUNT(1) FROM ({baseSql}{whereSql}) ForgeSearchCount";
    }

    private string NextParameterName(string column)
    {
        var clean = new string(column.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(clean))
            clean = "p";

        return clean + _parameterIndex++;
    }

    private void Merge(object? parameters)
    {
        if (parameters is null)
            return;

        if (parameters is IReadOnlyDictionary<string, object?> dictionary)
        {
            foreach (var item in dictionary)
                _parameters[item.Key] = item.Value;
            return;
        }

        foreach (var property in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            _parameters[property.Name] = property.GetValue(parameters);
    }
}

public sealed class ForgeProcedureSearch<T>
{
    private readonly ForgeDb _db;
    private readonly string _procedureName;
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private int? _page;
    private int? _pageSize;

    /// <summary>
    /// Initializes or executes the ForgeProcedureSearch operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="procedureName">The procedureName value.</param>
    public ForgeProcedureSearch(ForgeDb db, string procedureName)
    {
        _db = db;
        _procedureName = procedureName;
    }

    /// <summary>
    /// Initializes or executes the With operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The operation result.</returns>
    public ForgeProcedureSearch<T> With(string name, object? value)
    {
        _parameters[Normalize(name)] = value;
        return this;
    }

    /// <summary>
    /// Initializes or executes the WithOptional operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The operation result.</returns>
    public ForgeProcedureSearch<T> WithOptional(string name, object? value)
    {
        if (value is not null)
            With(name, value);

        return this;
    }

    /// <summary>
    /// Initializes or executes the Page operation.
    /// </summary>
    /// <param name="page">The page value.</param>
    /// <param name="pageSize">The pageSize value.</param>
    /// <returns>The operation result.</returns>
    public ForgeProcedureSearch<T> Page(int page, int pageSize)
    {
        _page = Math.Max(page, 1);
        _pageSize = Math.Max(pageSize, 1);
        _parameters["Page"] = _page.Value;
        _parameters["PageSize"] = _pageSize.Value;
        return this;
    }

    /// <summary>
    /// Initializes or executes the ToListAsync operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public Task<IReadOnlyList<T>> ToListAsync()
    {
        return _db.QueryProcedureAsync<T>(_procedureName, _parameters);
    }

    /// <summary>
    /// Initializes or executes the ToPagedAsync operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public async Task<ForgePagedResult<T>> ToPagedAsync()
    {
        var items = await ToListAsync();

        return new ForgePagedResult<T>
        {
            Items = items,
            Page = _page ?? 1,
            PageSize = _pageSize ?? items.Count,
            TotalRecords = items.Count
        };
    }

    private static string Normalize(string name) => name.TrimStart('@', ':');
}

public sealed class ForgePagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalRecords { get; init; }
    /// <summary>
    /// Executes the : operation.
    /// </summary>
    /// <returns>The operation result.</returns>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalRecords / (double)PageSize);
}

internal sealed record ForgeSearchExpressionResult(string Sql, IReadOnlyDictionary<string, object?> Parameters);

internal static class ForgeSearchExpression
{
    /// <summary>
    /// Initializes or executes the Translate operation.
    /// </summary>
    /// <param name="expression">The expression value.</param>
    /// <param name="startIndex">The startIndex value.</param>
    /// <returns>The operation result.</returns>
    public static ForgeSearchExpressionResult Translate<T>(Expression<Func<T, bool>> expression, int startIndex)
    {
        var parameters = new Dictionary<string, object?>();
        var sql = TranslateNode(expression.Body, parameters, ref startIndex);
        return new ForgeSearchExpressionResult(sql, parameters);
    }

    private static string TranslateNode(Expression expression, Dictionary<string, object?> parameters, ref int index)
    {
        if (expression is BinaryExpression binary)
        {
            if (binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                var left = TranslateNode(binary.Left, parameters, ref index);
                var right = TranslateNode(binary.Right, parameters, ref index);
                var op = binary.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({left}) {op} ({right})";
            }

            var member = Member(binary.Left);
            var parameter = AddParameter(Evaluate(binary.Right), parameters, ref index);
            return $"{member} {Operator(binary.NodeType)} {parameter}";
        }

        throw new NotSupportedException("Only simple search expressions are supported.");
    }

    private static string Member(Expression expression)
    {
        return expression is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Left side must be a member expression.");
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

    private static string AddParameter(object? value, Dictionary<string, object?> parameters, ref int index)
    {
        var name = "s" + index++;
        parameters[name] = value;
        return "@" + name;
    }

    private static object? Evaluate(Expression expression)
    {
        return Expression.Lambda(expression).Compile().DynamicInvoke();
    }
}
