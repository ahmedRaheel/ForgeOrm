using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
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
    public static ValueTask<ForgeDataFrame> ReadCsvAsync(string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromCsvAsync(path, hasHeader, delimiter, cancellationToken);

    /// <summary>
    /// Executes the ReadCsvAsync operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="hasHeader">The hasHeader value.</param>
    /// <param name="delimiter">The delimiter value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadCsvAsync operation.</returns>
    public static ValueTask<ForgeDataFrame> ReadCsvAsync(Stream stream, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)
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
    public static ValueTask<ForgeDataFrame> ReadJsonAsync(string path, CancellationToken cancellationToken = default)
        => ForgeDataFrame.FromJsonAsync(path, cancellationToken);

    /// <summary>
    /// Executes the ReadJsonAsync operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadJsonAsync operation.</returns>
    public static ValueTask<ForgeDataFrame> ReadJsonAsync(Stream stream, CancellationToken cancellationToken = default)
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

    internal ForgeFrameQuery(ForgeDb db) => _db = db;

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

    /// <summary>
    /// Adds a SQL WHERE predicate generated from a simple expression such as x => x.CreatedAt >= from.
    /// </summary>
    /// <param name="predicate">The expression predicate.</param>
    /// <returns>The current query.</returns>
    public ForgeFrameQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        _where.Add(ForgeFrameExpressionSql.ToSql(predicate.Body));
        return this;
    }

    internal bool ParallelExecutionEnabled { get; private set; }

    internal int MaxParallelism { get; private set; } = Environment.ProcessorCount;

    internal void EnableParallelExecution()
    {
        ParallelExecutionEnabled = true;
    }

    internal void SetMaxDegreeOfParallelism(int degree)
    {
        if (degree <= 0)
            throw new ArgumentOutOfRangeException(nameof(degree), "Max degree of parallelism must be greater than zero.");

        MaxParallelism = degree;
    }

    /// <summary>
    /// Sums a numeric column selected by expression. Honors Parallel()/MaxDegreeOfParallelism().
    /// </summary>
    public async ValueTask<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
    {
        var frame = await ToFrameAsync(cancellationToken).ConfigureAwait(false);
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        var values = frame.Rows.Select(row => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(row, column))).Where(x => x.HasValue).Select(x => x!.Value);

        if (!ParallelExecutionEnabled)
            return values.Sum();

        var chunks = values.Chunk(2048);
        decimal total = 0;
        object gate = new();

        await System.Threading.Tasks.Parallel.ForEachAsync(
            chunks,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxParallelism,
                CancellationToken = cancellationToken
            },
            (chunk, token) =>
            {
                decimal local = 0;
                foreach (var value in chunk)
                    local += value;

                lock (gate)
                    total += local;

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

        return total;
    }

    /// <summary>
    /// Sums a nullable numeric column selected by expression. Honors Parallel()/MaxDegreeOfParallelism().
    /// </summary>
    public async ValueTask<decimal> SumAsync(Expression<Func<T, decimal?>> selector, CancellationToken cancellationToken = default)
    {
        var frame = await ToFrameAsync(cancellationToken).ConfigureAwait(false);
        var column = ForgeFrameExpressionSql.GetMemberName(selector.Body);
        var values = frame.Rows.Select(row => ForgeDataFrame.ToDecimal(ForgeDataFrame.Get(row, column))).Where(x => x.HasValue).Select(x => x!.Value);

        if (!ParallelExecutionEnabled)
            return values.Sum();

        var chunks = values.Chunk(2048);
        decimal total = 0;
        object gate = new();

        await System.Threading.Tasks.Parallel.ForEachAsync(
            chunks,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxParallelism,
                CancellationToken = cancellationToken
            },
            (chunk, token) =>
            {
                decimal local = 0;
                foreach (var value in chunk)
                    local += value;

                lock (gate)
                    total += local;

                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

        return total;
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
    public async ValueTask<ForgeDataFrame> ToFrameAsync(CancellationToken cancellationToken = default)
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
    public static string ToSql(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => $"{ToSql(binary.Left)} {ToOperator(binary.NodeType)} {ToSql(binary.Right)}",
            MemberExpression member when member.Expression is not null && member.Expression.NodeType == ExpressionType.Parameter => member.Member.Name,
            MemberExpression member => FormatValue(Evaluate(member)),
            ConstantExpression constant => FormatValue(constant.Value),
            UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked => ToSql(unary.Operand),
            MethodCallExpression call when call.Method.Name == nameof(string.Contains) && call.Object is not null =>
                $"{ToSql(call.Object)} LIKE '%' + {ToSql(call.Arguments[0])} + '%'",
            MethodCallExpression call when call.Method.Name == nameof(string.StartsWith) && call.Object is not null =>
                $"{ToSql(call.Object)} LIKE {ToSql(call.Arguments[0])} + '%'",
            MethodCallExpression call when call.Method.Name == nameof(string.EndsWith) && call.Object is not null =>
                $"{ToSql(call.Object)} LIKE '%' + {ToSql(call.Arguments[0])}",
            _ => throw new NotSupportedException($"ForgeFrame expression is not supported: {expression}")
        };
    }

    public static string GetMemberName(Expression expression)
    {
        expression = StripConvert(expression);
        if (expression is MemberExpression member)
            return member.Member.Name;

        throw new NotSupportedException($"Only direct member selectors are supported by ForgeFrame aggregate expressions: {expression}");
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression unary && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
            expression = unary.Operand;
        return expression;
    }

    private static string ToOperator(ExpressionType type) => type switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "<>",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        ExpressionType.AndAlso => "AND",
        ExpressionType.OrElse => "OR",
        _ => throw new NotSupportedException($"ForgeFrame operator is not supported: {type}")
    };

    private static object? Evaluate(Expression expression)
    {
        var lambda = Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object)));
        return lambda.Compile().Invoke();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => "'" + s.Replace("'", "''") + "'",
            Guid g => "'" + g + "'",
            DateTime dt => "'" + dt.ToString("O", CultureInfo.InvariantCulture) + "'",
            DateTimeOffset dto => "'" + dto.ToString("O", CultureInfo.InvariantCulture) + "'",
            bool b => b ? "1" : "0",
            Enum e => Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => "'" + Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("'", "''") + "'"
        };
    }
}
