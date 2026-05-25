using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.Core;

namespace ForgeORM.Analytics;

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
    public async ValueTask<string> ToDynamicSqlServerPivotScriptAsync(CancellationToken cancellationToken = default)
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
