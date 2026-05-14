using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Abstractions;

namespace ForgeORM.Core.Search;

/// <summary>
/// Public ForgeORM search API. This belongs to the ForgeORM library, not sample projects.
/// Samples should only compose searches by calling db.Search&lt;T&gt;().
/// </summary>
public static class ForgeSearchExtensions
{
    public static ForgeSearch<T> Search<T>(this IForgeDb db) => new(db);
    public static ForgeProcedureSearch<T> SearchProcedure<T>(this IForgeDb db, string procedureName) => new(db, procedureName);
}

public sealed class ForgeSearch<T>
{
    private readonly IForgeDb _db;
    private readonly List<string> _columns = [];
    private readonly List<string> _where = [];
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private int _parameterIndex;
    private string? _fromSql;
    private string? _table;
    private string? _orderBy;
    private int? _page;
    private int? _pageSize;

    public ForgeSearch(IForgeDb db) => _db = db;

    public ForgeSearch<T> Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    public ForgeSearch<T> Select(params Expression<Func<T, object?>>[] columns)
    {
        _columns.AddRange(columns.Select(ForgeSearchExpression.MemberName));
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
        => condition ? Where(predicate) : this;

    /// <summary>Advanced escape hatch for hand-written SQL predicates.</summary>
    public ForgeSearch<T> Where(string sql, object? parameters = null)
    {
        _where.Add(sql);
        Merge(parameters);
        return this;
    }

    /// <summary>Advanced escape hatch for optional hand-written SQL predicates.</summary>
    public ForgeSearch<T> WhereIf(bool condition, string sql, object? parameters = null)
        => condition ? Where(sql, parameters) : this;

    public ForgeSearch<T> Optional<TValue>(Expression<Func<T, TValue>> column, TValue? value)
    {
        if (value is null) return this;
        return Optional(ForgeSearchExpression.MemberName(column), value);
    }

    public ForgeSearch<T> Optional<TValue>(string column, TValue? value)
    {
        if (value is null) return this;
        var name = NextParameterName(column);
        _where.Add($"{column} = @{name}");
        _parameters[name] = ForgeSearchExpression.NormalizeParameterValue(value);
        return this;
    }

    public ForgeSearch<T> OptionalLike(Expression<Func<T, string?>> column, string? value)
        => OptionalLike(ForgeSearchExpression.MemberName(column), value);

    public ForgeSearch<T> OptionalLike(string column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return this;
        var name = NextParameterName(column);
        _where.Add($"{column} LIKE @{name}");
        _parameters[name] = $"%{value}%";
        return this;
    }

    public ForgeSearch<T> OptionalBetween<TValue>(Expression<Func<T, TValue>> column, TValue? from, TValue? to)
        => OptionalBetween(ForgeSearchExpression.MemberName(column), from, to);

    public ForgeSearch<T> OptionalBetween<TValue>(string column, TValue? from, TValue? to)
    {
        if (from is not null)
        {
            var name = NextParameterName(column + "From");
            _where.Add($"{column} >= @{name}");
            _parameters[name] = ForgeSearchExpression.NormalizeParameterValue(from);
        }
        if (to is not null)
        {
            var name = NextParameterName(column + "To");
            _where.Add($"{column} <= @{name}");
            _parameters[name] = ForgeSearchExpression.NormalizeParameterValue(to);
        }
        return this;
    }

    public ForgeSearch<T> OrderBy(Expression<Func<T, object?>> column)
    {
        _orderBy = ForgeSearchExpression.MemberName(column) + " ASC";
        return this;
    }

    public ForgeSearch<T> OrderByDescending(Expression<Func<T, object?>> column)
    {
        _orderBy = ForgeSearchExpression.MemberName(column) + " DESC";
        return this;
    }

    /// <summary>Advanced escape hatch for hand-written ORDER BY clauses.</summary>
    public ForgeSearch<T> OrderBy(string orderBy)
    {
        _orderBy = orderBy;
        return this;
    }

    public ForgeSearch<T> Page(int page, int pageSize)
    {
        _page = Math.Max(page, 1);
        _pageSize = Math.Max(pageSize, 1);
        return this;
    }

    public ForgeRenderedSearchSql Render()
    {
        var sql = new StringBuilder(BuildBaseSql());
        if (_where.Count > 0) sql.Append(" WHERE ").Append(string.Join(" AND ", _where));
        if (!string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY ").Append(_orderBy);
        if (_page.HasValue && _pageSize.HasValue)
        {
            if (string.IsNullOrWhiteSpace(_orderBy)) sql.Append(" ORDER BY 1");
            var skip = (_page.Value - 1) * _pageSize.Value;
            sql.Append($" OFFSET {skip} ROWS FETCH NEXT {_pageSize.Value} ROWS ONLY");
        }
        return new ForgeRenderedSearchSql(sql.ToString(), _parameters);
    }

    public async Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var query = Render();
        return await _db.QueryAsync<T>(query.Sql, query.Parameters, cancellationToken: cancellationToken);
    }

    public async Task<ForgePagedResult<T>> ToPagedAsync(CancellationToken cancellationToken = default)
    {
        var dataQuery = Render();
        var total = await _db.ExecuteScalarAsync<int>(BuildCountSql(), _parameters, cancellationToken: cancellationToken);
        var items = await _db.QueryAsync<T>(dataQuery.Sql, dataQuery.Parameters, cancellationToken: cancellationToken);
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
        if (!string.IsNullOrWhiteSpace(_fromSql)) return _fromSql.Trim();
        var table = string.IsNullOrWhiteSpace(_table) ? ForgeSearchExpression.ResolveTableName(typeof(T)) : _table;
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
        return (string.IsNullOrWhiteSpace(clean) ? "p" : clean) + _parameterIndex++;
    }

    private void Merge(object? parameters)
    {
        if (parameters is null) return;
        if (parameters is IReadOnlyDictionary<string, object?> dictionary)
        {
            foreach (var item in dictionary) _parameters[item.Key] = ForgeSearchExpression.NormalizeParameterValue(item.Value);
            return;
        }
        foreach (var property in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            _parameters[property.Name] = ForgeSearchExpression.NormalizeParameterValue(property.GetValue(parameters), property);
    }
}

public sealed class ForgeProcedureSearch<T>
{
    private readonly IForgeDb _db;
    private readonly string _procedureName;
    private readonly Dictionary<string, object?> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private int? _page;
    private int? _pageSize;

    public ForgeProcedureSearch(IForgeDb db, string procedureName)
    {
        _db = db;
        _procedureName = procedureName;
    }

    public ForgeProcedureSearch<T> With(string name, object? value)
    {
        _parameters[Normalize(name)] = ForgeSearchExpression.NormalizeParameterValue(value);
        return this;
    }

    public ForgeProcedureSearch<T> With<TValue>(Expression<Func<T, TValue>> parameter, TValue? value)
        => With(ForgeSearchExpression.MemberName(parameter), value);

    public ForgeProcedureSearch<T> WithOptional(string name, object? value)
    {
        if (value is not null) With(name, value);
        return this;
    }

    public ForgeProcedureSearch<T> WithOptional<TValue>(Expression<Func<T, TValue>> parameter, TValue? value)
    {
        if (value is not null) With(parameter, value);
        return this;
    }

    public ForgeProcedureSearch<T> Page(int page, int pageSize)
    {
        _page = Math.Max(page, 1);
        _pageSize = Math.Max(pageSize, 1);
        _parameters["Page"] = _page.Value;
        _parameters["PageSize"] = _pageSize.Value;
        return this;
    }

    public Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
        => _db.QueryProcedureAsync<T>(_procedureName, _parameters, cancellationToken: cancellationToken);

    public async Task<ForgePagedResult<T>> ToPagedAsync(CancellationToken cancellationToken = default)
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

    private static string Normalize(string name) => name.TrimStart('@', ':');
}

public sealed record ForgeRenderedSearchSql(string Sql, object? Parameters = null);

internal sealed record ForgeSearchExpressionResult(string Sql, IReadOnlyDictionary<string, object?> Parameters);

internal static class ForgeSearchExpression
{
    public static ForgeSearchExpressionResult Translate<T>(Expression<Func<T, bool>> expression, int startIndex)
    {
        var parameters = new Dictionary<string, object?>();
        var sql = TranslateNode(expression.Body, parameters, ref startIndex);
        return new ForgeSearchExpressionResult(sql, parameters);
    }

    public static string MemberName<T, TValue>(Expression<Func<T, TValue>> expression)
        => MemberName(expression.Body);

    public static string MemberName(Expression expression)
    {
        expression = StripConvert(expression);
        return expression is MemberExpression member
            ? ColumnName(member.Member)
            : throw new NotSupportedException($"Expression must point to a member. Found: {expression.NodeType} - {expression}");
    }

    public static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttribute<ForgeTableAttribute>();
        if (attr is not null) return attr.Name;
        return type.Name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? type.Name : type.Name + "s";
    }

    public static object? NormalizeParameterValue(object? value, MemberInfo? member = null)
    {
        if (value is null) return null;
        var valueType = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
        if (!valueType.IsEnum) return value;

        var storage = member?.GetCustomAttribute<ForgeEnumStorageAttribute>()?.Storage ?? ForgeEnumStorage.String;
        return storage == ForgeEnumStorage.Number ? Convert.ToInt64(value) : value.ToString();
    }

    private static string TranslateNode(Expression expression, Dictionary<string, object?> parameters, ref int index)
    {
        expression = StripConvert(expression);

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
            var value = Evaluate(binary.Right);
            var parameter = AddParameter(NormalizeParameterValue(value, member.Member), parameters, ref index);
            return $"{ColumnName(member.Member)} {Operator(binary.NodeType)} {parameter}";
        }

        if (expression is MethodCallExpression call)
            return TranslateMethodCall(call, parameters, ref index);

        throw new NotSupportedException($"Only simple search expressions are supported. Found: {expression.NodeType} - {expression}");
    }

    private static string TranslateMethodCall(MethodCallExpression call, Dictionary<string, object?> parameters, ref int index)
    {
        if (call.Method.Name == nameof(string.Contains) && call.Object is not null)
        {
            var member = Member(call.Object);
            var value = Evaluate(call.Arguments[0]);
            var parameter = AddParameter($"%{value}%", parameters, ref index);
            return $"{ColumnName(member.Member)} LIKE {parameter}";
        }

        if (call.Method.Name == nameof(string.StartsWith) && call.Object is not null)
        {
            var member = Member(call.Object);
            var value = Evaluate(call.Arguments[0]);
            var parameter = AddParameter($"{value}%", parameters, ref index);
            return $"{ColumnName(member.Member)} LIKE {parameter}";
        }

        if (call.Method.Name == nameof(string.EndsWith) && call.Object is not null)
        {
            var member = Member(call.Object);
            var value = Evaluate(call.Arguments[0]);
            var parameter = AddParameter($"%{value}", parameters, ref index);
            return $"{ColumnName(member.Member)} LIKE {parameter}";
        }

        throw new NotSupportedException($"Search method {call.Method.Name} is not supported yet.");
    }

    private static MemberExpression Member(Expression expression)
    {
        expression = StripConvert(expression);
        return expression is MemberExpression member
            ? member
            : throw new NotSupportedException($"Left side must be a member expression. Found: {expression.NodeType} - {expression}");
    }

    private static string ColumnName(MemberInfo member)
        => member.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? member.Name;

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
        expression = StripConvert(expression);
        return Expression.Lambda(expression).Compile().DynamicInvoke();
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression unary &&
               (unary.NodeType == ExpressionType.Convert ||
                unary.NodeType == ExpressionType.ConvertChecked ||
                unary.NodeType == ExpressionType.TypeAs))
        {
            expression = unary.Operand;
        }

        return expression;
    }
}
