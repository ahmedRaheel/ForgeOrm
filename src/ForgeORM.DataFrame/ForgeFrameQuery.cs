using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

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
