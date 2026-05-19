using System.Collections;
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

public sealed partial class ForgeFrameQuery<T>
{
    private readonly ForgeDb _db;
    private string? _sql;
    private object? _parameters;
    private string? _table;
    private readonly List<string> _where = [];
    private string? _orderBy;
    private bool _parallel;
    private int _maxDegreeOfParallelism = Environment.ProcessorCount;

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
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="orderBy">The orderBy value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public ForgeFrameQuery<T> OrderBy(string orderBy) { _orderBy = orderBy; return this; }


    internal void EnableParallelExecution() => _parallel = true;
    internal void SetMaxDegreeOfParallelism(int degree) => _maxDegreeOfParallelism = Math.Max(1, degree);

    /// <summary>Expression-based frame filter using the same SQL translator as Set&lt;T&gt;().Where(...).</summary>
    public ForgeFrameQuery<T> Where(Expression<Func<T, bool>> predicate)
    {
        _where.Add(ForgeExpressionTranslator.Translate(predicate));
        return this;
    }

    /// <summary>Aggregates a decimal column directly from the database. Parallel mode is preserved for future provider parallel plans.</summary>
    public Task<decimal> SumAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("SUM", selector, cancellationToken);

    public Task<decimal> AverageAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("AVG", selector, cancellationToken);

    public Task<decimal> MinAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("MIN", selector, cancellationToken);

    public Task<decimal> MaxAsync(Expression<Func<T, decimal>> selector, CancellationToken cancellationToken = default)
        => ExecuteDecimalAggregateAsync("MAX", selector, cancellationToken);

    private Task<decimal> ExecuteDecimalAggregateAsync(string aggregate, LambdaExpression selector, CancellationToken cancellationToken)
    {
        var column = ForgeExpressionTranslator.MemberName(selector);
        var sql = $"SELECT COALESCE({aggregate}({column}), 0) FROM ({BuildSql()}) ForgeFrameAggregate";
        return _db.ExecuteScalarAsync<decimal>(sql, _parameters, cancellationToken: cancellationToken);
    }

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

    private string BuildSql()
    {
        var sql = !string.IsNullOrWhiteSpace(_sql) ? _sql! : $"SELECT * FROM {_table ?? typeof(T).Name}";
        if (_where.Count > 0) sql += " WHERE " + string.Join(" AND ", _where);
        if (!string.IsNullOrWhiteSpace(_orderBy)) sql += " ORDER BY " + _orderBy;
        return sql;
    }
}
