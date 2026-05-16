using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Analytics;

public static class ForgeAnalyticsExtensions
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeAnalyticsQuery<T> Analytics<T>(this ForgeDb db) => new(db);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeAnalyticsQuery<T> Analytics<T>(this ForgeDbContext db) => new(db);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgePivotQuery<T> Pivot<T>(this ForgeDb db) => new(db);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgePivotQuery<T> Pivot<T>(this ForgeDbContext db) => new(db);
}

public sealed class ForgeAnalyticsQuery<T>
{
    private readonly ForgeDb _db;
    private readonly List<string> _selects = [];
    private readonly List<string> _where = [];
    private readonly List<string> _groupBy = [];
    private readonly List<string> _orderBy = [];
    private string? _from;

    internal ForgeAnalyticsQuery(ForgeDb db) => _db = db;

    /// <summary>
    /// Executes the From operation.
    /// </summary>
    /// <param name="tableOrView">The tableOrView value.</param>
    /// <returns>The result of the From operation.</returns>
    public ForgeAnalyticsQuery<T> From(string tableOrView)
    {
        _from = tableOrView;
        return this;
    }

    /// <summary>
    /// Executes the WhereSql operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the WhereSql operation.</returns>
    public ForgeAnalyticsQuery<T> WhereSql(string sql)
    {
        if (!string.IsNullOrWhiteSpace(sql)) _where.Add(sql);
        return this;
    }

    /// <summary>
    /// Executes the Select operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the Select operation.</returns>
    public ForgeAnalyticsQuery<T> Select(Expression<Func<T, object?>> column, string? alias = null)
    {
        var name = Column(column);
        _selects.Add(alias is null ? name : $"{name} AS [{alias}]");
        return this;
    }

    /// <summary>
    /// Executes the SelectSql operation.
    /// </summary>
    /// <param name="sql">The sql value.</param>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the SelectSql operation.</returns>
    public ForgeAnalyticsQuery<T> SelectSql(string sql, string? alias = null)
    {
        _selects.Add(alias is null ? sql : $"{sql} AS [{alias}]");
        return this;
    }

    /// <summary>
    /// Executes the GroupBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the GroupBy operation.</returns>
    public ForgeAnalyticsQuery<T> GroupBy(params Expression<Func<T, object?>>[] columns)
    {
        _groupBy.AddRange(columns.Select(Column));
        return this;
    }

    /// <summary>
    /// Executes the GroupBySql operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the GroupBySql operation.</returns>
    public ForgeAnalyticsQuery<T> GroupBySql(params string[] columns)
    {
        _groupBy.AddRange(columns.Where(x => !string.IsNullOrWhiteSpace(x)));
        return this;
    }

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public ForgeAnalyticsQuery<T> OrderBy(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => Column(c) + " ASC"));
        return this;
    }

    /// <summary>
    /// Executes the OrderByDescending operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
    public ForgeAnalyticsQuery<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => Column(c) + " DESC"));
        return this;
    }

    /// <summary>
    /// Executes the OrderBySql operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the OrderBySql operation.</returns>
    public ForgeAnalyticsQuery<T> OrderBySql(params string[] columns)
    {
        _orderBy.AddRange(columns.Where(x => !string.IsNullOrWhiteSpace(x)));
        return this;
    }

    internal ForgeAnalyticsQuery<T> AddSelect(string sql)
    {
        _selects.Add(sql);
        return this;
    }

    internal void AddProjection(string sql) => _selects.Add(sql);

    // Ranking / distribution window functions
    /// <summary>
    /// Executes the RowNumber operation.
    /// </summary>
    /// <returns>The result of the RowNumber operation.</returns>
    public ForgeWindowMetric<T> RowNumber() => new(this, "ROW_NUMBER()", null);
    /// <summary>
    /// Executes the Rank operation.
    /// </summary>
    /// <returns>The result of the Rank operation.</returns>
    public ForgeWindowMetric<T> Rank() => new(this, "RANK()", null);
    /// <summary>
    /// Executes the DenseRank operation.
    /// </summary>
    /// <returns>The result of the DenseRank operation.</returns>
    public ForgeWindowMetric<T> DenseRank() => new(this, "DENSE_RANK()", null);
    /// <summary>
    /// Executes the Ntile operation.
    /// </summary>
    /// <param name="buckets">The buckets value.</param>
    /// <returns>The result of the Ntile operation.</returns>
    public ForgeWindowMetric<T> Ntile(int buckets) => new(this, $"NTILE({buckets})", null);
    /// <summary>
    /// Executes the PercentRank operation.
    /// </summary>
    /// <returns>The result of the PercentRank operation.</returns>
    public ForgeWindowMetric<T> PercentRank() => new(this, "PERCENT_RANK()", null);
    /// <summary>
    /// Executes the CumeDist operation.
    /// </summary>
    /// <returns>The result of the CumeDist operation.</returns>
    public ForgeWindowMetric<T> CumeDist() => new(this, "CUME_DIST()", null);

    // Aggregate window functions
    /// <summary>
    /// Executes the Count operation.
    /// </summary>
    /// <returns>The result of the Count operation.</returns>
    public ForgeWindowMetric<T> Count() => new(this, "COUNT(*)", null);
    /// <summary>
    /// Executes the Sum operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the Sum operation.</returns>
    public ForgeWindowMetric<T> Sum(Expression<Func<T, object?>> column) => new(this, $"SUM({Column(column)})", null);
    /// <summary>
    /// Executes the Avg operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the Avg operation.</returns>
    public ForgeWindowMetric<T> Avg(Expression<Func<T, object?>> column) => new(this, $"AVG({Column(column)})", null);
    /// <summary>
    /// Executes the Min operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the Min operation.</returns>
    public ForgeWindowMetric<T> Min(Expression<Func<T, object?>> column) => new(this, $"MIN({Column(column)})", null);
    /// <summary>
    /// Executes the Max operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the Max operation.</returns>
    public ForgeWindowMetric<T> Max(Expression<Func<T, object?>> column) => new(this, $"MAX({Column(column)})", null);

    // Analytic value functions
    /// <summary>
    /// Executes the Lag operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="offset">The offset value.</param>
    /// <returns>The result of the Lag operation.</returns>
    public ForgeWindowMetric<T> Lag(Expression<Func<T, object?>> column, int offset = 1) => new(this, $"LAG({Column(column)}, {offset})", null);
    /// <summary>
    /// Executes the Lead operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="offset">The offset value.</param>
    /// <returns>The result of the Lead operation.</returns>
    public ForgeWindowMetric<T> Lead(Expression<Func<T, object?>> column, int offset = 1) => new(this, $"LEAD({Column(column)}, {offset})", null);
    /// <summary>
    /// Executes the FirstValue operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the FirstValue operation.</returns>
    public ForgeWindowMetric<T> FirstValue(Expression<Func<T, object?>> column) => new(this, $"FIRST_VALUE({Column(column)})", null);
    /// <summary>
    /// Executes the LastValue operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the LastValue operation.</returns>
    public ForgeWindowMetric<T> LastValue(Expression<Func<T, object?>> column) => new(this, $"LAST_VALUE({Column(column)})", null);

    // Percentile functions
    /// <summary>
    /// Executes the PercentileCont operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="percentile">The percentile value.</param>
    /// <param name="sqlType">The sqlType value.</param>
    /// <param name="castType">The castType value.</param>
    /// <returns>The result of the PercentileCont operation.</returns>
    public ForgeWindowMetric<T> PercentileCont(
        Expression<Func<T, object?>> column,
        decimal percentile,
        string sqlType = "decimal(18,6)",
        string castType = "decimal(18,4)")
    {
        var c = Column(column);
        var p = percentile.ToString(CultureInfo.InvariantCulture);
        return new(this,
            $"CAST(PERCENTILE_CONT({p}) WITHIN GROUP (ORDER BY TRY_CONVERT({sqlType}, {c}))",
            $" AS {castType})");
    }

    /// <summary>
    /// Executes the PercentileDisc operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <param name="percentile">The percentile value.</param>
    /// <param name="sqlType">The sqlType value.</param>
    /// <param name="castType">The castType value.</param>
    /// <returns>The result of the PercentileDisc operation.</returns>
    public ForgeWindowMetric<T> PercentileDisc(
        Expression<Func<T, object?>> column,
        decimal percentile,
        string sqlType = "decimal(18,6)",
        string castType = "decimal(18,4)")
    {
        var c = Column(column);
        var p = percentile.ToString(CultureInfo.InvariantCulture);
        return new(this,
            $"CAST(PERCENTILE_DISC({p}) WITHIN GROUP (ORDER BY TRY_CONVERT({sqlType}, {c}))",
            $" AS {castType})");
    }

    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <returns>The result of the Render operation.</returns>
    public ForgeRenderedAnalyticsSql Render()
    {
        var from = _from ?? ResolveTableName(typeof(T));
        var sb = new StringBuilder();

        sb.Append("SELECT ");
        sb.Append(_selects.Count == 0 ? "*" : string.Join(", ", _selects));
        sb.Append(" FROM ").Append(from);

        if (_where.Count > 0)
            sb.Append(" WHERE ").Append(string.Join(" AND ", _where));

        if (_groupBy.Count > 0)
            sb.Append(" GROUP BY ").Append(string.Join(", ", _groupBy));

        if (_orderBy.Count > 0)
            sb.Append(" ORDER BY ").Append(string.Join(", ", _orderBy));

        return new ForgeRenderedAnalyticsSql(sb.ToString());
    }

    /// <summary>
    /// Executes the TResult operation.
    /// </summary>
    /// <typeparam name="TResult">The type used by the operation.</typeparam>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the TResult operation.</returns>
    public Task<IReadOnlyList<TResult>> ToListAsync<TResult>(CancellationToken cancellationToken = default)
    {
        var sql = Render().Sql;
        return _db.QueryAsync<TResult>(sql, cancellationToken: cancellationToken);
    }
    /// <summary>
    /// Executes the ToDynamicListAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToDynamicListAsync operation.</returns>
    public async Task<IReadOnlyList<IDictionary<string, object?>>> ToDynamicListAsync(
        CancellationToken cancellationToken = default)
    {
        var render = Render();

        return await _db.QueryDynamicAsync(
            sql: render.Sql,
            parameters: null,
            cancellationToken: cancellationToken);
    }
    internal static string Column(Expression<Func<T, object?>> expression)
    {
        Expression e = expression.Body;
        while (e is UnaryExpression u) e = u.Operand;

        return e is MemberExpression m && m.Member is PropertyInfo p
            ? ResolveColumnName(p)
            : throw new NotSupportedException("Only member expressions are supported.");
    }

    internal static string ResolveTableName(Type type)
        => type.GetCustomAttribute<ForgeTableAttribute>()?.Name
           ?? (type.Name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? type.Name : type.Name + "s");

    internal static string ResolveColumnName(PropertyInfo property)
        => property.GetCustomAttribute<ForgeColumnAttribute>()?.Name ?? property.Name;
}

public sealed class ForgeWindowMetric<T>
{
    private readonly ForgeAnalyticsQuery<T> _query;
    private readonly string _function;
    private readonly string? _suffix;
    private readonly List<string> _partitionBy = [];
    private readonly List<string> _orderBy = [];
    private string? _frame;

    internal ForgeWindowMetric(ForgeAnalyticsQuery<T> query, string function, string? suffix)
    {
        _query = query;
        _function = function;
        _suffix = suffix;
    }

    /// <summary>
    /// Executes the PartitionBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the PartitionBy operation.</returns>
    public ForgeWindowMetric<T> PartitionBy(params Expression<Func<T, object?>>[] columns)
    {
        _partitionBy.AddRange(columns.Select(ForgeAnalyticsQuery<T>.Column));
        return this;
    }

    /// <summary>
    /// Executes the PartitionBySql operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the PartitionBySql operation.</returns>
    public ForgeWindowMetric<T> PartitionBySql(params string[] columns)
    {
        _partitionBy.AddRange(columns.Where(x => !string.IsNullOrWhiteSpace(x)));
        return this;
    }

    /// <summary>
    /// Executes the OrderBy operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the OrderBy operation.</returns>
    public ForgeWindowMetric<T> OrderBy(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => ForgeAnalyticsQuery<T>.Column(c) + " ASC"));
        return this;
    }

    /// <summary>
    /// Executes the OrderByDescending operation.
    /// </summary>
    /// <param name="object">The object value.</param>
    /// <returns>The result of the OrderByDescending operation.</returns>
    public ForgeWindowMetric<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => ForgeAnalyticsQuery<T>.Column(c) + " DESC"));
        return this;
    }

    /// <summary>
    /// Executes the OrderBySql operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the OrderBySql operation.</returns>
    public ForgeWindowMetric<T> OrderBySql(params string[] columns)
    {
        _orderBy.AddRange(columns.Where(x => !string.IsNullOrWhiteSpace(x)));
        return this;
    }

    /// <summary>
    /// Executes the OverAll operation.
    /// </summary>
    /// <returns>The result of the OverAll operation.</returns>
    public ForgeWindowMetric<T> OverAll()
    {
        _partitionBy.Clear();
        _orderBy.Clear();
        _frame = null;
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenUnboundedPrecedingAndCurrentRow operation.
    /// </summary>
    /// <returns>The result of the RowsBetweenUnboundedPrecedingAndCurrentRow operation.</returns>
    public ForgeWindowMetric<T> RowsBetweenUnboundedPrecedingAndCurrentRow()
    {
        _frame = "ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW";
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenUnboundedPrecedingAndUnboundedFollowing operation.
    /// </summary>
    /// <returns>The result of the RowsBetweenUnboundedPrecedingAndUnboundedFollowing operation.</returns>
    public ForgeWindowMetric<T> RowsBetweenUnboundedPrecedingAndUnboundedFollowing()
    {
        _frame = "ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING";
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenPrecedingAndCurrentRow operation.
    /// </summary>
    /// <param name="preceding">The preceding value.</param>
    /// <returns>The result of the RowsBetweenPrecedingAndCurrentRow operation.</returns>
    public ForgeWindowMetric<T> RowsBetweenPrecedingAndCurrentRow(int preceding)
    {
        _frame = $"ROWS BETWEEN {preceding} PRECEDING AND CURRENT ROW";
        return this;
    }

    /// <summary>
    /// Executes the RowsBetweenCurrentRowAndFollowing operation.
    /// </summary>
    /// <param name="following">The following value.</param>
    /// <returns>The result of the RowsBetweenCurrentRowAndFollowing operation.</returns>
    public ForgeWindowMetric<T> RowsBetweenCurrentRowAndFollowing(int following)
    {
        _frame = $"ROWS BETWEEN CURRENT ROW AND {following} FOLLOWING";
        return this;
    }

    /// <summary>
    /// Executes the As operation.
    /// </summary>
    /// <param name="alias">The alias value.</param>
    /// <returns>The result of the As operation.</returns>
    public ForgeAnalyticsQuery<T> As(string alias)
    {
        var over = new List<string>();

        if (_partitionBy.Count > 0)
            over.Add("PARTITION BY " + string.Join(", ", _partitionBy));

        if (_orderBy.Count > 0)
            over.Add("ORDER BY " + string.Join(", ", _orderBy));

        if (!string.IsNullOrWhiteSpace(_frame))
            over.Add(_frame);

        var sql = $"{_function} OVER ({string.Join(" ", over)}){_suffix} AS [{alias}]";
        return _query.AddSelect(sql);
    }
}

public sealed class ForgePivotQuery<T>
{
    private readonly ForgeDb _db;
    private string? _from;
    private string? _row;
    private string? _column;
    private string? _value;
    private ForgeSqlAggregate _aggregate = ForgeSqlAggregate.Sum;
    private readonly List<string> _knownColumns = [];

    internal ForgePivotQuery(ForgeDb db) => _db = db;

    /// <summary>
    /// Executes the From operation.
    /// </summary>
    /// <param name="tableOrView">The tableOrView value.</param>
    /// <returns>The result of the From operation.</returns>
    public ForgePivotQuery<T> From(string tableOrView)
    {
        _from = tableOrView;
        return this;
    }

    /// <summary>
    /// Executes the Rows operation.
    /// </summary>
    /// <param name="row">The row value.</param>
    /// <returns>The result of the Rows operation.</returns>
    public ForgePivotQuery<T> Rows(Expression<Func<T, object?>> row)
    {
        _row = ForgeAnalyticsQuery<T>.Column(row);
        return this;
    }

    /// <summary>
    /// Executes the Columns operation.
    /// </summary>
    /// <param name="column">The column value.</param>
    /// <returns>The result of the Columns operation.</returns>
    public ForgePivotQuery<T> Columns(Expression<Func<T, object?>> column)
    {
        _column = ForgeAnalyticsQuery<T>.Column(column);
        return this;
    }

    /// <summary>
    /// Executes the Values operation.
    /// </summary>
    /// <param name="value">The value value.</param>
    /// <returns>The result of the Values operation.</returns>
    public ForgePivotQuery<T> Values(Expression<Func<T, object?>> value)
    {
        _value = ForgeAnalyticsQuery<T>.Column(value);
        return this;
    }

    /// <summary>
    /// Executes the Aggregate operation.
    /// </summary>
    /// <param name="aggregate">The aggregate value.</param>
    /// <returns>The result of the Aggregate operation.</returns>
    public ForgePivotQuery<T> Aggregate(ForgeSqlAggregate aggregate)
    {
        _aggregate = aggregate;
        return this;
    }

    /// <summary>
    /// Executes the KnownColumns operation.
    /// </summary>
    /// <param name="stringcolumns">The stringcolumns value.</param>
    /// <returns>The result of the KnownColumns operation.</returns>
    public ForgePivotQuery<T> KnownColumns(params string[] columns)
    {
        _knownColumns.AddRange(columns);
        return this;
    }

    /// <summary>
    /// Executes the Render operation.
    /// </summary>
    /// <returns>The result of the Render operation.</returns>
    public ForgeRenderedAnalyticsSql Render()
    {
        if (_row is null || _column is null || _value is null)
            throw new InvalidOperationException("Rows, Columns and Values are required for pivot.");

        var from = _from ?? ForgeAnalyticsQuery<T>.ResolveTableName(typeof(T));
        var cols = _knownColumns.Count == 0
            ? "/* dynamic: supply KnownColumns(...) or use ToDynamicSqlServerPivotScriptAsync */"
            : string.Join(", ", _knownColumns.Select(c => $"[{c.Replace("]", "]]" )}]"));

        var agg = _aggregate switch
        {
            ForgeSqlAggregate.Sum => "SUM",
            ForgeSqlAggregate.Avg => "AVG",
            ForgeSqlAggregate.Count => "COUNT",
            ForgeSqlAggregate.Min => "MIN",
            ForgeSqlAggregate.Max => "MAX",
            _ => "SUM"
        };

        var sql = $"SELECT * FROM (SELECT {_row}, {_column}, {_value} AS MetricValue FROM {from}) src PIVOT ({agg}(MetricValue) FOR {_column} IN ({cols})) p";
        return new ForgeRenderedAnalyticsSql(sql);
    }

    /// <summary>
    /// Executes the ToDynamicSqlServerPivotScriptAsync operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToDynamicSqlServerPivotScriptAsync operation.</returns>
    public async Task<string> ToDynamicSqlServerPivotScriptAsync(CancellationToken cancellationToken = default)
    {
        if (_row is null || _column is null || _value is null)
            throw new InvalidOperationException("Rows, Columns and Values are required for pivot.");

        var from = _from ?? ForgeAnalyticsQuery<T>.ResolveTableName(typeof(T));
        var colSql = $"SELECT DISTINCT CAST({_column} AS nvarchar(4000)) FROM {from} WHERE {_column} IS NOT NULL ORDER BY 1";
        var columns = await _db.QueryAsync<string>(colSql, cancellationToken: cancellationToken);
        KnownColumns(columns.ToArray());
        return Render().Sql;
    }
}

public enum ForgeSqlAggregate
{
    Sum,
    Avg,
    Count,
    Min,
    Max
}

public sealed record ForgeRenderedAnalyticsSql(string Sql);
