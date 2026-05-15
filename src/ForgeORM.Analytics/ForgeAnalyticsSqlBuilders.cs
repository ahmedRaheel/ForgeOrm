using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Analytics;

public static class ForgeAnalyticsExtensions
{
    public static ForgeAnalyticsQuery<T> Analytics<T>(this ForgeDb db) => new(db);
    public static ForgeAnalyticsQuery<T> Analytics<T>(this ForgeDbContext db) => new(db);
    public static ForgePivotQuery<T> Pivot<T>(this ForgeDb db) => new(db);
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

    public ForgeAnalyticsQuery<T> From(string tableOrView)
    {
        _from = tableOrView;
        return this;
    }

    public ForgeAnalyticsQuery<T> WhereSql(string sql)
    {
        if (!string.IsNullOrWhiteSpace(sql)) _where.Add(sql);
        return this;
    }

    public ForgeAnalyticsQuery<T> Select(Expression<Func<T, object?>> column, string? alias = null)
    {
        var name = Column(column);
        _selects.Add(alias is null ? name : $"{name} AS [{alias}]");
        return this;
    }

    public ForgeAnalyticsQuery<T> SelectSql(string sql, string? alias = null)
    {
        _selects.Add(alias is null ? sql : $"{sql} AS [{alias}]");
        return this;
    }

    public ForgeAnalyticsQuery<T> GroupBy(params Expression<Func<T, object?>>[] columns)
    {
        _groupBy.AddRange(columns.Select(Column));
        return this;
    }

    public ForgeAnalyticsQuery<T> GroupBySql(params string[] columns)
    {
        _groupBy.AddRange(columns.Where(x => !string.IsNullOrWhiteSpace(x)));
        return this;
    }

    public ForgeAnalyticsQuery<T> OrderBy(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => Column(c) + " ASC"));
        return this;
    }

    public ForgeAnalyticsQuery<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => Column(c) + " DESC"));
        return this;
    }

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
    public ForgeWindowMetric<T> RowNumber() => new(this, "ROW_NUMBER()", null);
    public ForgeWindowMetric<T> Rank() => new(this, "RANK()", null);
    public ForgeWindowMetric<T> DenseRank() => new(this, "DENSE_RANK()", null);
    public ForgeWindowMetric<T> Ntile(int buckets) => new(this, $"NTILE({buckets})", null);
    public ForgeWindowMetric<T> PercentRank() => new(this, "PERCENT_RANK()", null);
    public ForgeWindowMetric<T> CumeDist() => new(this, "CUME_DIST()", null);

    // Aggregate window functions
    public ForgeWindowMetric<T> Count() => new(this, "COUNT(*)", null);
    public ForgeWindowMetric<T> Sum(Expression<Func<T, object?>> column) => new(this, $"SUM({Column(column)})", null);
    public ForgeWindowMetric<T> Avg(Expression<Func<T, object?>> column) => new(this, $"AVG({Column(column)})", null);
    public ForgeWindowMetric<T> Min(Expression<Func<T, object?>> column) => new(this, $"MIN({Column(column)})", null);
    public ForgeWindowMetric<T> Max(Expression<Func<T, object?>> column) => new(this, $"MAX({Column(column)})", null);

    // Analytic value functions
    public ForgeWindowMetric<T> Lag(Expression<Func<T, object?>> column, int offset = 1) => new(this, $"LAG({Column(column)}, {offset})", null);
    public ForgeWindowMetric<T> Lead(Expression<Func<T, object?>> column, int offset = 1) => new(this, $"LEAD({Column(column)}, {offset})", null);
    public ForgeWindowMetric<T> FirstValue(Expression<Func<T, object?>> column) => new(this, $"FIRST_VALUE({Column(column)})", null);
    public ForgeWindowMetric<T> LastValue(Expression<Func<T, object?>> column) => new(this, $"LAST_VALUE({Column(column)})", null);

    // Percentile functions
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

    public Task<IReadOnlyList<TResult>> ToListAsync<TResult>(CancellationToken cancellationToken = default)
    {
        var sql = Render().Sql;
        return _db.QueryAsync<TResult>(sql, cancellationToken: cancellationToken);
    }
    public async Task<IReadOnlyList<IDictionary<string, object?>>> ToDynamicListAsync(
    CancellationToken cancellationToken = default)
    {
        var render = Render();

        return await _db.QueryDynamicAsync(
            render.Sql,           
            cancellationToken);
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

    public ForgeWindowMetric<T> PartitionBy(params Expression<Func<T, object?>>[] columns)
    {
        _partitionBy.AddRange(columns.Select(ForgeAnalyticsQuery<T>.Column));
        return this;
    }

    public ForgeWindowMetric<T> PartitionBySql(params string[] columns)
    {
        _partitionBy.AddRange(columns.Where(x => !string.IsNullOrWhiteSpace(x)));
        return this;
    }

    public ForgeWindowMetric<T> OrderBy(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => ForgeAnalyticsQuery<T>.Column(c) + " ASC"));
        return this;
    }

    public ForgeWindowMetric<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)
    {
        _orderBy.AddRange(columns.Select(c => ForgeAnalyticsQuery<T>.Column(c) + " DESC"));
        return this;
    }

    public ForgeWindowMetric<T> OrderBySql(params string[] columns)
    {
        _orderBy.AddRange(columns.Where(x => !string.IsNullOrWhiteSpace(x)));
        return this;
    }

    public ForgeWindowMetric<T> OverAll()
    {
        _partitionBy.Clear();
        _orderBy.Clear();
        _frame = null;
        return this;
    }

    public ForgeWindowMetric<T> RowsBetweenUnboundedPrecedingAndCurrentRow()
    {
        _frame = "ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW";
        return this;
    }

    public ForgeWindowMetric<T> RowsBetweenUnboundedPrecedingAndUnboundedFollowing()
    {
        _frame = "ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING";
        return this;
    }

    public ForgeWindowMetric<T> RowsBetweenPrecedingAndCurrentRow(int preceding)
    {
        _frame = $"ROWS BETWEEN {preceding} PRECEDING AND CURRENT ROW";
        return this;
    }

    public ForgeWindowMetric<T> RowsBetweenCurrentRowAndFollowing(int following)
    {
        _frame = $"ROWS BETWEEN CURRENT ROW AND {following} FOLLOWING";
        return this;
    }

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

    public ForgePivotQuery<T> From(string tableOrView)
    {
        _from = tableOrView;
        return this;
    }

    public ForgePivotQuery<T> Rows(Expression<Func<T, object?>> row)
    {
        _row = ForgeAnalyticsQuery<T>.Column(row);
        return this;
    }

    public ForgePivotQuery<T> Columns(Expression<Func<T, object?>> column)
    {
        _column = ForgeAnalyticsQuery<T>.Column(column);
        return this;
    }

    public ForgePivotQuery<T> Values(Expression<Func<T, object?>> value)
    {
        _value = ForgeAnalyticsQuery<T>.Column(value);
        return this;
    }

    public ForgePivotQuery<T> Aggregate(ForgeSqlAggregate aggregate)
    {
        _aggregate = aggregate;
        return this;
    }

    public ForgePivotQuery<T> KnownColumns(params string[] columns)
    {
        _knownColumns.AddRange(columns);
        return this;
    }

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
