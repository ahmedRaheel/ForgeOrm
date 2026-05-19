using System.Collections;
using System.Reflection;
using System.Linq.Expressions;
using System.Globalization;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

public static class ForgeFrameExtensions
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeFrameQuery<T> Frame<T>(this ForgeDb db) => new(db);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeFrameQuery<T> Frame<T>(this ForgeDbContext db) => new(db);

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="rows">The rows value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeDataFrame ToForgeFrame<T>(this IEnumerable<T> rows)
        => new(rows.Select(ToDictionary));

    /// <summary>
    /// Executes the ReadCsv operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <returns>The result of the ReadCsv operation.</returns>
    public static ForgeDataFrame ReadCsv(string path, bool hasHeader = true, char delimiter = ',')
        => ForgeDataFrame.FromCsv(path, hasHeader, delimiter);

    /// <summary>
    /// Executes the ReadCsvAsync operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadCsvAsync operation.</returns>
    public static Task<ForgeDataFrame> ReadCsvAsync(string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromCsvAsync(path, hasHeader, delimiter, cancellationToken);

    /// <summary>
    /// Executes the ReadCsvAsync operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadCsvAsync operation.</returns>
    public static Task<ForgeDataFrame> ReadCsvAsync(Stream stream, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromCsvAsync(stream, hasHeader, delimiter, cancellationToken);

    /// <summary>
    /// Executes the ReadJson operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <returns>The result of the ReadJson operation.</returns>
    public static ForgeDataFrame ReadJson(string path)
        => ForgeDataFrame.FromJson(path);

    /// <summary>
    /// Executes the ReadJsonAsync operation.
    /// </summary>
    /// <param name="path">The path value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadJsonAsync operation.</returns>
    public static Task<ForgeDataFrame> ReadJsonAsync(string path, CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromJsonAsync(path, cancellationToken);

    /// <summary>
    /// Executes the ReadJsonAsync operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadJsonAsync operation.</returns>
    public static Task<ForgeDataFrame> ReadJsonAsync(Stream stream, CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromJsonAsync(stream, cancellationToken);

    private static IDictionary<string, object?> ToDictionary<T>(T row)
    {
        if (row is IDictionary<string, object?> dict) return new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase);
        if (row is IDictionary nonGeneric)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry item in nonGeneric) result[item.Key.ToString() ?? string.Empty] = item.Value;
            return result;
        }

        return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, p => p.GetValue(row), StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class ForgeFrameQuery<T>
{
    private readonly ForgeDb _db;
    private string? _sql;
    private object? _parameters;
    private string? _table;
    private readonly List<string> _where = [];
    private string? _orderBy;
    internal bool ParallelExecutionEnabled { get; private set; }
    internal int MaxParallelism { get; private set; } = Math.Max(1, Environment.ProcessorCount);

    internal ForgeFrameQuery(ForgeDb db) => _db = db;

    internal void EnableParallelExecution() => ParallelExecutionEnabled = true;

    internal void SetMaxDegreeOfParallelism(int degree)
    {
        if (degree <= 0) throw new ArgumentOutOfRangeException(nameof(degree), "Degree of parallelism must be greater than zero.");
        MaxParallelism = degree;
    }

    /// <summary>
    /// Executes the From operation.
    /// </summary>
    /// <param name="table">The table value.</param>
    /// <returns>The result of the From operation.</returns>
    public ForgeFrameQuery<T> From(string table) { _table = table; return this; }
    /// <summary>
    /// Executes the FromSql operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the FromSql operation.</returns>
    public ForgeFrameQuery<T> FromSql(string sql, object? parameters = null) { _sql = sql; _parameters = parameters; return this; }
    /// <summary>
    /// Executes the WhereSql operation.
    /// </summary>
    /// <param name="_where">The _where value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    public ForgeFrameQuery<T> WhereSql(string condition) { _where.Add(condition); return this; }

    /// <summary>Applies an expression-based filter to the frame query.</summary>
    public ForgeFrameQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _where.Add(ForgeFrameExpressionSql.Translate(predicate));
        return this;
    }
    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public ForgeFrameQuery<T> OrderBy(string orderBy) { _orderBy = orderBy; return this; }

    /// <summary>
    /// Executes the ToFrameAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToFrameAsync operation.</returns>
    public async Task<ForgeDataFrame> ToFrameAsync(CancellationToken cancellationToken = default)
    {
        var sql = BuildSql();
        var rows = await _db.QueryDynamicAsync(sql: sql, parameters: _parameters, cancellationToken: cancellationToken);
        return new ForgeDataFrame(rows);
    }

    /// <summary>
    /// Executes the ToFrame operation.
    /// </summary>
    /// <returns>The result of the ToFrame operation.</returns>
    public ForgeDataFrame ToFrame()
    {
        var rows = _db.QueryDynamicAsync(sql: BuildSql(), parameters: _parameters).GetAwaiter().GetResult();
        return new ForgeDataFrame(rows);
    }

    /// <summary>Executes SUM for a decimal selector. Parallel mode is honoured for in-memory frames.</summary>
    public async Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var column = ForgeFrameExpressionSql.MemberName(selector);
        if (!ParallelExecutionEnabled)
        {
            var sql = $"SELECT SUM({column}) AS Value FROM ({BuildSql()}) q";
            return await _db.ExecuteScalarAsync<decimal>(sql, parameters: _parameters, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var frame = await ToFrameAsync(cancellationToken).ConfigureAwait(false);
        return frame.Vectorized<T>().MaxDegreeOfParallelism(MaxParallelism).Sum(selector);
    }

    /// <summary>Executes AVG for a decimal selector.</summary>
    public async Task<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var column = ForgeFrameExpressionSql.MemberName(selector);
        var sql = $"SELECT AVG({column}) AS Value FROM ({BuildSql()}) q";
        return await _db.ExecuteScalarAsync<decimal>(sql, parameters: _parameters, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Executes MIN for a decimal selector.</summary>
    public async Task<decimal> MinAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var column = ForgeFrameExpressionSql.MemberName(selector);
        var sql = $"SELECT MIN({column}) AS Value FROM ({BuildSql()}) q";
        return await _db.ExecuteScalarAsync<decimal>(sql, parameters: _parameters, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Executes MAX for a decimal selector.</summary>
    public async Task<decimal> MaxAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var column = ForgeFrameExpressionSql.MemberName(selector);
        var sql = $"SELECT MAX({column}) AS Value FROM ({BuildSql()}) q";
        return await _db.ExecuteScalarAsync<decimal>(sql, parameters: _parameters, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private string BuildSql()
    {
        var sql = !string.IsNullOrWhiteSpace(_sql) ? _sql! : $"SELECT * FROM {_table ?? typeof(T).Name}";
        if (_where.Count > 0) sql += " WHERE " + string.Join(" AND ", _where);
        if (!string.IsNullOrWhiteSpace(_orderBy)) sql += " ORDER BY " + _orderBy;
        return sql;
    }
}


internal static class ForgeFrameExpressionSql
{
    public static string Translate<T>(Expression<Func<T, bool>> expression) => TranslateNode(expression.Body);

    public static string MemberName<T>(Expression<Func<T, decimal>> selector)
    {
        Expression body = selector.Body;
        if (body is UnaryExpression unary) body = unary.Operand;
        if (body is MemberExpression member) return member.Member.Name;
        throw new NotSupportedException("Only simple member selectors are supported, for example x => x.GrandTotal.");
    }

    private static string TranslateNode(Expression expression)
    {
        if (expression is BinaryExpression binary)
        {
            var left = TranslateNode(binary.Left);
            var right = TranslateNode(binary.Right);
            var op = binary.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                _ => throw new NotSupportedException($"Expression operator '{binary.NodeType}' is not supported for frame SQL.")
            };
            return binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse
                ? $"({left} {op} {right})"
                : $"{left} {op} {right}";
        }

        if (expression is MemberExpression member)
        {
            if (member.Expression?.NodeType == ExpressionType.Parameter) return member.Member.Name;
            return FormatConstant(Evaluate(expression));
        }

        if (expression is ConstantExpression constant) return FormatConstant(constant.Value);
        if (expression is UnaryExpression unary) return TranslateNode(unary.Operand);

        throw new NotSupportedException($"Expression node '{expression.NodeType}' is not supported for frame SQL.");
    }

    private static object? Evaluate(Expression expression)
        => Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile().Invoke();

    private static string FormatConstant(object? value)
    {
        if (value is null) return "NULL";
        if (value is string s) return "'" + s.Replace("'", "''") + "'";
        if (value is DateTime dt) return "'" + dt.ToString("O", CultureInfo.InvariantCulture) + "'";
        if (value is DateTimeOffset dto) return "'" + dto.ToString("O", CultureInfo.InvariantCulture) + "'";
        if (value is bool b) return b ? "1" : "0";
        if (value is Enum e) return Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "NULL";
    }
}
