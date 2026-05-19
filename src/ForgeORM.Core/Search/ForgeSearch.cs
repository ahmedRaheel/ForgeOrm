using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Core;
using ForgeORM.QueryAst;

namespace ForgeORM.Core.Search;

/// <summary>
/// Entry-point extensions for enterprise search.
/// </summary>
public static class ForgeSearchExtensions
{
    /// <summary>
    /// Starts a dynamic search query.
    /// </summary>
    public static ForgeSearch<T> Search<T>(this ForgeDb db)
    {
        return new ForgeSearch<T>(db);
    }

    /// <summary>
    /// Starts a stored-procedure-backed search query.
    /// </summary>
    public static ForgeProcedureSearch<T> SearchProcedure<T>(
        this ForgeDb db,
        string procedureName)
    {
        return new ForgeProcedureSearch<T>(db, procedureName);
    }
}

/// <summary>
/// Dynamic enterprise search builder. This belongs in the package, not the sample.
/// </summary>
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

    public ForgeSearch(ForgeDb db)
    {
        _db = db;
    }

    public ForgeSearch<T> Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    public ForgeSearch<T> Select(params Expression<Func<T, object?>>[] columns)
    {
        _columns.AddRange(columns.Select(ForgeSearchExpression.Column));
        return this;
    }

    public ForgeSearch<T> From(string table)
    {
        _table = table;
        return this;
    }

    public ForgeSearch<T> FromSql(string sql)
    {
        _fromSql = sql;
        return this;
    }

    public ForgeSearch<T> Where(Expression<Func<T, bool>> predicate)
    {
        var translated = ForgeSearchExpression.Translate(predicate, _parameterIndex);
        _parameterIndex += translated.Parameters.Count;
        _where.Add(translated.Sql);
        Merge(translated.Parameters);
        return this;
    }

    public ForgeSearch<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? Where(predicate) : this;
    }

    public ForgeSearch<T> Where(string sql, object? parameters = null)
    {
        _where.Add(sql);
        Merge(parameters);
        return this;
    }

    public ForgeSearch<T> WhereIf(bool condition, string sql, object? parameters = null)
    {
        return condition ? Where(sql, parameters) : this;
    }

    public ForgeSearch<T> Optional<TValue>(string column, TValue? value)
    {
        if (value is null)
        {
            return this;
        }

        var name = NextParameterName(column);
        _where.Add($"{column} = @{name}");
        _parameters[name] = value;
        return this;
    }

    public ForgeSearch<T> Optional<TValue>(
        Expression<Func<T, TValue>> column,
        TValue? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, int>> column,
        int? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, int?>> column,
        int? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, long>> column,
        long? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, long?>> column,
        long? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, Guid>> column,
        Guid? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, Guid?>> column,
        Guid? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, decimal>> column,
        decimal? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, decimal?>> column,
        decimal? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, DateTime>> column,
        DateTime? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, DateTime?>> column,
        DateTime? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, DateTimeOffset>> column,
        DateTimeOffset? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> Optional(
        Expression<Func<T, DateTimeOffset?>> column,
        DateTimeOffset? value)
    {
        return Optional(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> OptionalLike(string column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        var name = NextParameterName(column);
        _where.Add($"{column} LIKE @{name}");
        _parameters[name] = $"%{value}%";
        return this;
    }

    public ForgeSearch<T> OptionalLike(
        Expression<Func<T, string?>> column,
        string? value)
    {
        return OptionalLike(ForgeSearchExpression.Column(column), value);
    }

    public ForgeSearch<T> OptionalBetween<TValue>(
        string column,
        TValue? from,
        TValue? to)
        where TValue : struct
    {
        if (from is not null)
        {
            var name = NextParameterName(column + "From");
            _where.Add($"{column} >= @{name}");
            _parameters[name] = from.Value;
        }

        if (to is not null)
        {
            var name = NextParameterName(column + "To");
            _where.Add($"{column} <= @{name}");
            _parameters[name] = to.Value;
        }

        return this;
    }

    public ForgeSearch<T> OptionalBetween<TValue>(
        Expression<Func<T, TValue>> column,
        TValue? from,
        TValue? to)
        where TValue : struct
    {
        return OptionalBetween(
            ForgeSearchExpression.Column(column),
            from,
            to);
    }

    public ForgeSearch<T> OptionalBetween<TValue>(
        Expression<Func<T, TValue?>> column,
        TValue? from,
        TValue? to)
        where TValue : struct
    {
        return OptionalBetween(
            ForgeSearchExpression.Column(column),
            from,
            to);
    }

    public ForgeSearch<T> OrderBy(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    public ForgeSearch<T> OrderBy<TValue>(
        Expression<Func<T, TValue>> column,
        bool descending = false)
    {
        _orderBy = ForgeSearchExpression.Column(column) + (descending ? " DESC" : " ASC");
        return this;
    }

    public ForgeSearch<T> OrderByDescending<TValue>(
        Expression<Func<T, TValue>> column)
    {
        return OrderBy(column, descending: true);
    }

    public ForgeSearch<T> Page(int page, int pageSize)
    {
        _page = Math.Max(page, 1);
        _pageSize = Math.Max(pageSize, 1);
        return this;
    }

    public ForgeRenderedSql Render()
    {
        var sql = new StringBuilder();
        var baseSql = BuildBaseSql();

        sql.Append(baseSql);

        if (_where.Count > 0)
        {
            sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        }

        if (!string.IsNullOrWhiteSpace(_orderBy))
        {
            sql.Append(" ORDER BY ").Append(_orderBy);
        }

        if (_page.HasValue && _pageSize.HasValue)
        {
            if (string.IsNullOrWhiteSpace(_orderBy))
            {
                sql.Append(" ORDER BY 1");
            }

            var skip = (_page.Value - 1) * _pageSize.Value;
            sql.Append($" OFFSET {skip} ROWS FETCH NEXT {_pageSize.Value} ROWS ONLY");
        }

        return new ForgeRenderedSql(sql.ToString(), _parameters);
    }

    public string ToSql() => Render().Sql;

    public async Task<IReadOnlyList<T>> ToListAsync(
        CancellationToken cancellationToken = default)
    {
        var query = Render();

        return await _db.QueryAsync<T>(
            query.Sql,
            query.Parameters,
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> ToDictionaryAsync(
        CancellationToken cancellationToken = default)
    {
        var query = Render();

        return await _db.QueryDictionaryAsync(
            query.Sql,
            query.Parameters,
            cancellationToken);
    }

    public async Task<ForgePagedResult<T>> ToPagedAsync(
        CancellationToken cancellationToken = default)
    {
        var dataQuery = Render();
        var countSql = BuildCountSql();
        var total = await _db.ExecuteScalarAsync<int>(
            countSql,
            _parameters,
            cancellationToken: cancellationToken);

        var items = await _db.QueryAsync<T>(
            dataQuery.Sql,
            dataQuery.Parameters,
            cancellationToken: cancellationToken);

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
        {
            return _fromSql.Trim();
        }

        var table = string.IsNullOrWhiteSpace(_table)
            ? typeof(T).Name
            : _table;

        var columns = _columns.Count == 0
            ? "*"
            : string.Join(", ", _columns);

        return $"SELECT {columns} FROM {table}";
    }

    private string BuildCountSql()
    {
        var baseSql = BuildBaseSql();
        var whereSql = _where.Count == 0
            ? string.Empty
            : " WHERE " + string.Join(" AND ", _where);

        return $"SELECT COUNT(1) FROM ({baseSql}{whereSql}) ForgeSearchCount";
    }

    private string NextParameterName(string column)
    {
        var clean = new string(column.Where(char.IsLetterOrDigit).ToArray());

        if (string.IsNullOrWhiteSpace(clean))
        {
            clean = "p";
        }

        return clean + _parameterIndex++;
    }

    private void Merge(object? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        if (parameters is IReadOnlyDictionary<string, object?> dictionary)
        {
            foreach (var item in dictionary)
            {
                _parameters[item.Key] = item.Value;
            }

            return;
        }

        foreach (var property in parameters
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            _parameters[property.Name] = ForgeRuntimeAccessorCache.Get(property, parameters);
        }
    }
}

/// <summary>
/// Stored-procedure-backed search builder.
/// </summary>
public sealed class ForgeProcedureSearch<T>
{
    private readonly ForgeDb _db;
    private readonly string _procedureName;
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private int? _page;
    private int? _pageSize;

    public ForgeProcedureSearch(
        ForgeDb db,
        string procedureName)
    {
        _db = db;
        _procedureName = procedureName;
    }

    public ForgeProcedureSearch<T> With(
        string name,
        object? value)
    {
        _parameters[Normalize(name)] = value;
        return this;
    }

    public ForgeProcedureSearch<T> WithOptional(
        string name,
        object? value)
    {
        if (value is not null)
        {
            With(name, value);
        }

        return this;
    }

    public ForgeProcedureSearch<T> Page(
        int page,
        int pageSize)
    {
        _page = Math.Max(page, 1);
        _pageSize = Math.Max(pageSize, 1);
        _parameters["Page"] = _page.Value;
        _parameters["PageSize"] = _pageSize.Value;
        return this;
    }

    public Task<IReadOnlyList<T>> ToListAsync(
        CancellationToken cancellationToken = default)
    {
        return _db.QueryProcedureAsync<T>(
            _procedureName,
            _parameters,
            cancellationToken: cancellationToken);
    }

    public async Task<ForgePagedResult<T>> ToPagedAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await ToListAsync(cancellationToken);

        return new ForgePagedResult<T>
        {
            Items = items,
            Page = _page ?? 1,
            PageSize = _pageSize ?? items.Count,
            TotalRecords = items.Count
        };
    }

    private static string Normalize(string name)
    {
        return name.TrimStart('@', ':');
    }
}

public sealed class ForgePagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalRecords { get; init; }

    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalRecords / (double)PageSize);
}

internal sealed record ForgeSearchExpressionResult(
    string Sql,
    IReadOnlyDictionary<string, object?> Parameters);

internal static class ForgeSearchExpression
{
    public static ForgeSearchExpressionResult Translate<T>(
        Expression<Func<T, bool>> expression,
        int startIndex)
    {
        var parameters = new Dictionary<string, object?>();
        var sql = TranslateNode(
            expression.Body,
            parameters,
            ref startIndex);

        return new ForgeSearchExpressionResult(
            sql,
            parameters);
    }

    public static string Column<T, TValue>(
        Expression<Func<T, TValue>> expression)
    {
        return Column(expression.Body);
    }

    public static string Column(Expression expression)
    {
        while (expression is UnaryExpression unary)
        {
            expression = unary.Operand;
        }

        return expression is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Expression must point to a property.");
    }

    private static string TranslateNode(
        Expression expression,
        Dictionary<string, object?> parameters,
        ref int index)
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

            var member = Column(binary.Left);
            var parameter = AddParameter(
                Evaluate(binary.Right),
                parameters,
                ref index);

            return $"{member} {Operator(binary.NodeType)} {parameter}";
        }

        throw new NotSupportedException("Only simple search expressions are supported.");
    }

    private static string Operator(ExpressionType type)
    {
        return type switch
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

    private static string AddParameter(
        object? value,
        Dictionary<string, object?> parameters,
        ref int index)
    {
        var name = "s" + index++;
        parameters[name] = value;
        return "@" + name;
    }

    private static object? Evaluate(Expression expression)
    {
        return ForgeExpressionDelegateCache.Evaluate(expression);
    }
}
