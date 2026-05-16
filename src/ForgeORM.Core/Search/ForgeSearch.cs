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
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeSearch<T> Search<T>(this IForgeDb db) => new(db);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <param name="procedureName">The procedureName value.</param>
    /// <returns>The result of the T operation.</returns>
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

    /// <summary>
    /// Executes the ForgeSearch operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the ForgeSearch operation.</returns>
    public ForgeSearch(IForgeDb db) => _db = db;

    /// <summary>
    /// Executes the Select operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the Select operation.</returns>
    public ForgeSearch<T> Select(params string[] columns)
    {
        _columns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Executes the Select operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the Select operation.</returns>
    public ForgeSearch<T> Select(params Expression<Func<T, object?>>[] columns)
    {
        _columns.AddRange(columns.Select(ForgeSearchExpression.MemberName));
        return this;
    }

    /// <summary>
    /// Executes the From operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the From operation.</returns>
    public ForgeSearch<T> From(string table)
    {
        _table = table;
        return this;
    }

    /// <summary>
    /// Executes the FromSql operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the FromSql operation.</returns>
    public ForgeSearch<T> FromSql(string sql)
    {
        _fromSql = sql;
        return this;
    }

    /// <summary>
    /// Executes the Where operation.
    /// </summary>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the Where operation.</returns>
    public ForgeSearch<T> Where(Expression<Func<T, bool>> predicate)
    {
        var translated = ForgeSearchExpression.Translate(predicate, _parameterIndex);
        _parameterIndex += translated.Parameters.Count;
        _where.Add(translated.Sql);
        Merge(translated.Parameters);
        return this;
    }

    /// <summary>
    /// Executes the WhereIf operation.
    /// </summary>
    /// <param name="condition">The condition value.</param>
    /// <param name="predicate">The predicate value.</param>
    /// <returns>The result of the WhereIf operation.</returns>
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

    /// <summary>
    /// Executes the TValue operation.
    /// </summary>
    /// <typeparam name="TValue">The type used by the operation.</typeparam>
    /// <param name="column">The column value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the TValue operation.</returns>
    public ForgeSearch<T> Optional<TValue>(Expression<Func<T, TValue>> column, TValue? value)
    {
        if (value is null) return this;
        return Optional(ForgeSearchExpression.MemberName(column), value);
    }

    /// <summary>
    /// Executes the TValue operation.
    /// </summary>
    /// <typeparam name="TValue">The type used by the operation.</typeparam>
    /// <param name="column">The column value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the TValue operation.</returns>
    public ForgeSearch<T> Optional<TValue>(string column, TValue? value)
    {
        if (value is null) return this;
        var name = NextParameterName(column);
        _where.Add($"{column} = @{name}");
        _parameters[name] = ForgeSearchExpression.NormalizeParameterValue(value);
        return this;
    }

    /// <summary>
    /// Executes the OptionalLike operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the OptionalLike operation.</returns>
    public ForgeSearch<T> OptionalLike(Expression<Func<T, string?>> column, string? value)
        => OptionalLike(ForgeSearchExpression.MemberName(column), value);

    /// <summary>
    /// Executes the OptionalLike operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the OptionalLike operation.</returns>
    public ForgeSearch<T> OptionalLike(string column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return this;
        var name = NextParameterName(column);
        _where.Add($"{column} LIKE @{name}");
        _parameters[name] = $"%{value}%";
        return this;
    }

    /// <summary>
    /// Executes the TValue operation.
    /// </summary>
    /// <typeparam name="TValue">The type used by the operation.</typeparam>
    /// <param name="column">The column value.</param>
    /// <param name="from">The from value.</param>
    /// <param name="to">The to value.</param>
    /// <returns>The result of the TValue operation.</returns>
    public ForgeSearch<T> OptionalBetween<TValue>(Expression<Func<T, TValue>> column, TValue? from, TValue? to)
        => OptionalBetween(ForgeSearchExpression.MemberName(column), from, to);

    /// <summary>
    /// Executes the TValue operation.
    /// </summary>
    /// <typeparam name="TValue">The type used by the operation.</typeparam>
    /// <param name="column">The column value.</param>
    /// <param name="from">The from value.</param>
    /// <param name="to">The to value.</param>
    /// <returns>The result of the TValue operation.</returns>
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

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public ForgeSearch<T> OrderBy(Expression<Func<T, object?>> column)
    {
        _orderBy = ForgeSearchExpression.MemberName(column) + " ASC";
        return this;
    }

    /// <summary>
    /// Executes the OrderByDescending operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
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

    /// <summary>
    /// Executes the Page operation.
    /// </summary>
    /// <param name="page">The page value.</param>
    /// <param name="pageSize">The pageSize value.</param>
    /// <returns>The result of the Page operation.</returns>
    public ForgeSearch<T> Page(int page, int pageSize)
    {
        _page = Math.Max(page, 1);
        _pageSize = Math.Max(pageSize, 1);
        return this;
    }

    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <returns>The result of the Render operation.</returns>
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

    /// <summary>
    /// Executes the ToListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    public async Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        var query = Render();
        return await _db.QueryAsync<T>(query.Sql, query.Parameters, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes the ToPagedAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToPagedAsync operation.</returns>
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

    /// <summary>
    /// Executes the ForgeProcedureSearch operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <param name="procedureName">The procedureName value.</param>
    /// <returns>The result of the ForgeProcedureSearch operation.</returns>
    public ForgeProcedureSearch(IForgeDb db, string procedureName)
    {
        _db = db;
        _procedureName = procedureName;
    }

    /// <summary>
    /// Executes the With operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the With operation.</returns>
    public ForgeProcedureSearch<T> With(string name, object? value)
    {
        _parameters[Normalize(name)] = ForgeSearchExpression.NormalizeParameterValue(value);
        return this;
    }

    /// <summary>
    /// Executes the TValue operation.
    /// </summary>
    /// <typeparam name="TValue">The type used by the operation.</typeparam>
    /// <param name="parameter">The parameter value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the TValue operation.</returns>
    public ForgeProcedureSearch<T> With<TValue>(Expression<Func<T, TValue>> parameter, TValue? value)
        => With(ForgeSearchExpression.MemberName(parameter), value);

    /// <summary>
    /// Executes the WithOptional operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the WithOptional operation.</returns>
    public ForgeProcedureSearch<T> WithOptional(string name, object? value)
    {
        if (value is not null) With(name, value);
        return this;
    }

    /// <summary>
    /// Executes the TValue operation.
    /// </summary>
    /// <typeparam name="TValue">The type used by the operation.</typeparam>
    /// <param name="parameter">The parameter value.</param>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the TValue operation.</returns>
    public ForgeProcedureSearch<T> WithOptional<TValue>(Expression<Func<T, TValue>> parameter, TValue? value)
    {
        if (value is not null) With(parameter, value);
        return this;
    }

    /// <summary>
    /// Executes the Page operation.
    /// </summary>
    /// <param name="page">The page value.</param>
    /// <param name="pageSize">The pageSize value.</param>
    /// <returns>The result of the Page operation.</returns>
    public ForgeProcedureSearch<T> Page(int page, int pageSize)
    {
        _page = Math.Max(page, 1);
        _pageSize = Math.Max(pageSize, 1);
        _parameters["Page"] = _page.Value;
        _parameters["PageSize"] = _pageSize.Value;
        return this;
    }

    /// <summary>
    /// Executes the ToListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    public Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)
        => _db.QueryProcedureAsync<T>(_procedureName, _parameters, cancellationToken: cancellationToken);

    /// <summary>
    /// Executes the ToPagedAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToPagedAsync operation.</returns>
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
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <param name="startIndex">The startIndex value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeSearchExpressionResult Translate<T>(Expression<Func<T, bool>> expression, int startIndex)
    {
        var parameters = new Dictionary<string, object?>();
        var sql = TranslateNode(expression.Body, parameters, ref startIndex);
        return new ForgeSearchExpressionResult(sql, parameters);
    }

    /// <summary>
    /// Executes the TValue operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <typeparam name="TValue">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the TValue operation.</returns>
    public static string MemberName<T, TValue>(Expression<Func<T, TValue>> expression)
        => MemberName(expression.Body);

    /// <summary>
    /// Executes the MemberName operation.
    /// </summary>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the MemberName operation.</returns>
    public static string MemberName(Expression expression)
    {
        expression = StripConvert(expression);
        return expression is MemberExpression member
            ? ColumnName(member.Member)
            : throw new NotSupportedException($"Expression must point to a member. Found: {expression.NodeType} - {expression}");
    }

    /// <summary>
    /// Executes the ResolveTableName operation.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the ResolveTableName operation.</returns>
    public static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttribute<ForgeTableAttribute>();
        if (attr is not null) return attr.Name;
        return type.Name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? type.Name : type.Name + "s";
    }

    /// <summary>
    /// Executes the NormalizeParameterValue operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <param name="member">The member value.</param>
    /// <returns>The result of the NormalizeParameterValue operation.</returns>
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
