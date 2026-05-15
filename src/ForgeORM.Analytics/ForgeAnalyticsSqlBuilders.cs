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
    private string? _from;
    private string? _where;

    internal ForgeAnalyticsQuery(ForgeDb db) => _db = db;

    public ForgeAnalyticsQuery<T> From(string tableOrView) { _from = tableOrView; return this; }
    public ForgeAnalyticsQuery<T> WhereSql(string sql) { _where = sql; return this; }
    public ForgeAnalyticsQuery<T> Select(Expression<Func<T, object?>> column, string? alias = null)
    {
        var name = Column(column);
        _selects.Add(alias is null ? name : $"{name} AS [{alias}]");
        return this;
    }
    public ForgeWindowMetric<T> RowNumber() => new(this, "ROW_NUMBER()", null);
    public ForgeWindowMetric<T> Count() => new(this, "COUNT(*)", null);
    public ForgeWindowMetric<T> Sum(Expression<Func<T, object?>> column) => new(this, $"SUM({Column(column)})", null);
    public ForgeWindowMetric<T> Avg(Expression<Func<T, object?>> column) => new(this, $"AVG({Column(column)})", null);
    public ForgeWindowMetric<T> PercentileCont(Expression<Func<T, object?>> column, decimal percentile, string sqlType = "decimal(18,6)", string castType = "decimal(18,4)")
    {
        var c = Column(column);
        return new(this, $"CAST(PERCENTILE_CONT({percentile.ToString(System.Globalization.CultureInfo.InvariantCulture)}) WITHIN GROUP (ORDER BY TRY_CONVERT({sqlType}, {c}))", $" AS {castType})");
    }

    internal ForgeAnalyticsQuery<T> AddSelect(string sql) { _selects.Add(sql); return this; }

    public ForgeRenderedAnalyticsSql Render()
    {
        var from = _from ?? ResolveTableName(typeof(T));
        var sb = new StringBuilder("SELECT ");
        sb.Append(_selects.Count == 0 ? "*" : string.Join(", ", _selects));
        sb.Append(" FROM ").Append(from);
        if (!string.IsNullOrWhiteSpace(_where)) sb.Append(" WHERE ").Append(_where);
        return new ForgeRenderedAnalyticsSql(sb.ToString());
    }

    public Task<IReadOnlyList<TResult>> ToListAsync<TResult>(CancellationToken cancellationToken = default)
    {
        var sql = Render().Sql;
        return _db.QueryAsync<TResult>(sql, cancellationToken: cancellationToken);
    }

    internal static string Column(Expression<Func<T, object?>> expression)
    {
        Expression e = expression.Body;
        while (e is UnaryExpression u) e = u.Operand;
        return e is MemberExpression m ? ResolveColumnName((PropertyInfo)m.Member) : throw new NotSupportedException("Only member expressions are supported.");
    }

    internal static string ResolveTableName(Type type)
        => type.GetCustomAttribute<ForgeTableAttribute>()?.Name ?? (type.Name.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? type.Name : type.Name + "s");

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

    internal ForgeWindowMetric(ForgeAnalyticsQuery<T> query, string function, string? suffix)
    {
        _query = query; _function = function; _suffix = suffix;
    }

    public ForgeWindowMetric<T> PartitionBy(params Expression<Func<T, object?>>[] columns) { _partitionBy.AddRange(columns.Select(ForgeAnalyticsQuery<T>.Column)); return this; }
    public ForgeWindowMetric<T> OrderBy(params Expression<Func<T, object?>>[] columns) { _orderBy.AddRange(columns.Select(ForgeAnalyticsQuery<T>.Column)); return this; }
    public ForgeWindowMetric<T> OrderBySql(string sql) { _orderBy.Add(sql); return this; }
    public ForgeWindowMetric<T> OverAll() => this;

    public ForgeAnalyticsQuery<T> As(string alias)
    {
        var over = new List<string>();
        if (_partitionBy.Count > 0) over.Add("PARTITION BY " + string.Join(", ", _partitionBy));
        if (_orderBy.Count > 0) over.Add("ORDER BY " + string.Join(", ", _orderBy));
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

    public ForgePivotQuery<T> From(string tableOrView) { _from = tableOrView; return this; }
    public ForgePivotQuery<T> Rows(Expression<Func<T, object?>> row) { _row = ForgeAnalyticsQuery<T>.Column(row); return this; }
    public ForgePivotQuery<T> Columns(Expression<Func<T, object?>> column) { _column = ForgeAnalyticsQuery<T>.Column(column); return this; }
    public ForgePivotQuery<T> Values(Expression<Func<T, object?>> value) { _value = ForgeAnalyticsQuery<T>.Column(value); return this; }
    public ForgePivotQuery<T> Aggregate(ForgeSqlAggregate aggregate) { _aggregate = aggregate; return this; }
    public ForgePivotQuery<T> KnownColumns(params string[] columns) { _knownColumns.AddRange(columns); return this; }

    public ForgeRenderedAnalyticsSql Render()
    {
        if (_row is null || _column is null || _value is null) throw new InvalidOperationException("Rows, Columns and Values are required for pivot.");
        var from = _from ?? ForgeAnalyticsQuery<T>.ResolveTableName(typeof(T));
        var cols = _knownColumns.Count == 0 ? "/* dynamic: supply KnownColumns(...) or use ToDynamicSqlServerPivotScriptAsync */" : string.Join(", ", _knownColumns.Select(c => $"[{c.Replace("]", "]]" )}]"));
        var agg = _aggregate switch { ForgeSqlAggregate.Sum => "SUM", ForgeSqlAggregate.Avg => "AVG", ForgeSqlAggregate.Count => "COUNT", ForgeSqlAggregate.Min => "MIN", ForgeSqlAggregate.Max => "MAX", _ => "SUM" };
        var sql = $"SELECT * FROM (SELECT {_row}, {_column}, {_value} AS MetricValue FROM {from}) src PIVOT ({agg}(MetricValue) FOR {_column} IN ({cols})) p";
        return new ForgeRenderedAnalyticsSql(sql);
    }

    public async Task<string> ToDynamicSqlServerPivotScriptAsync(CancellationToken cancellationToken = default)
    {
        if (_row is null || _column is null || _value is null) throw new InvalidOperationException("Rows, Columns and Values are required for pivot.");
        var from = _from ?? ForgeAnalyticsQuery<T>.ResolveTableName(typeof(T));
        var colSql = $"SELECT DISTINCT CAST({_column} AS nvarchar(4000)) FROM {from} WHERE {_column} IS NOT NULL ORDER BY 1";
        var columns = await _db.QueryAsync<string>(colSql, cancellationToken: cancellationToken);
        KnownColumns(columns.ToArray());
        return Render().Sql;
    }
}

public enum ForgeSqlAggregate { Sum, Avg, Count, Min, Max }
public sealed record ForgeRenderedAnalyticsSql(string Sql);
