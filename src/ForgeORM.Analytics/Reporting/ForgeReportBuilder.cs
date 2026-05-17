using System.Linq.Expressions;
using ForgeORM.Core;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Fluent report builder for enterprise reporting, pivots, drill-down, and exports.
/// Every major operation has both SQL-string and expression-based overloads.
/// </summary>
public sealed class ForgeReportBuilder<T>
{
    private readonly ForgeDb _db;
    private readonly ForgeReportDefinition _definition = new();

    internal ForgeReportBuilder(ForgeDb db, string? name = null)
    {
        _db = db;
        _definition.Name = name ?? typeof(T).Name + "Report";
    }

    public ForgeReportBuilder<T> From(string table)
    {
        _definition.Table = table;
        return this;
    }

    public ForgeReportBuilder<T> Dimension(string name, string expression)
    {
        _definition.Dimensions.Add(new ForgeReportDimension { Name = name, Expression = expression });
        return this;
    }

    public ForgeReportBuilder<T> Dimension<TValue>(string name, Expression<Func<T, TValue>> expression)
        => Dimension(name, ForgeReportExpressionHelpers.Column(expression));

    public ForgeReportBuilder<T> Year<TValue>(string name, Expression<Func<T, TValue>> expression)
        => Dimension(name, ForgeReportExpressionHelpers.Year(expression));

    public ForgeReportBuilder<T> Month<TValue>(string name, Expression<Func<T, TValue>> expression)
        => Dimension(name, ForgeReportExpressionHelpers.Month(expression));

    public ForgeReportBuilder<T> Day<TValue>(string name, Expression<Func<T, TValue>> expression)
        => Dimension(name, ForgeReportExpressionHelpers.Day(expression));

    public ForgeReportBuilder<T> Measure(ForgeReportMeasure measure)
    {
        _definition.Measures.Add(measure);
        return this;
    }

    public ForgeReportBuilder<T> Sum<TValue>(Expression<Func<T, TValue>> expression, string alias)
        => Measure(ForgeReportMeasure.Sum(ForgeReportExpressionHelpers.Column(expression), alias));

    public ForgeReportBuilder<T> Count<TValue>(Expression<Func<T, TValue>> expression, string alias)
        => Measure(ForgeReportMeasure.Count(ForgeReportExpressionHelpers.Column(expression), alias));

    public ForgeReportBuilder<T> Count(string alias = "Total")
        => Measure(ForgeReportMeasure.Count("1", alias));

    public ForgeReportBuilder<T> Average<TValue>(Expression<Func<T, TValue>> expression, string alias)
        => Measure(ForgeReportMeasure.Avg(ForgeReportExpressionHelpers.Column(expression), alias));

    public ForgeReportBuilder<T> Min<TValue>(Expression<Func<T, TValue>> expression, string alias)
        => Measure(ForgeReportMeasure.Min(ForgeReportExpressionHelpers.Column(expression), alias));

    public ForgeReportBuilder<T> Max<TValue>(Expression<Func<T, TValue>> expression, string alias)
        => Measure(ForgeReportMeasure.Max(ForgeReportExpressionHelpers.Column(expression), alias));

    public ForgeReportBuilder<T> Where(string sql, object? parameters = null)
    {
        _definition.WhereSql = sql;
        _definition.Parameters = parameters;
        return this;
    }

    public ForgeReportBuilder<T> OrderBy(string sql)
    {
        _definition.OrderBySql = sql;
        return this;
    }

    public ForgeReportBuilder<T> OrderBy<TValue>(Expression<Func<T, TValue>> expression, bool descending = false)
        => OrderBy(ForgeReportExpressionHelpers.Column(expression) + (descending ? " DESC" : " ASC"));

    public ForgeReportBuilder<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> expression)
        => OrderBy(expression, descending: true);

    public ForgeReportBuilder<T> TopN(string orderBySql, int count, bool descending = true)
    {
        _definition.Top = count;
        _definition.OrderBySql = orderBySql + (descending ? " DESC" : " ASC");
        return this;
    }

    public ForgeReportBuilder<T> TopN<TValue>(Expression<Func<T, TValue>> orderBy, int count, bool descending = true)
        => TopN(ForgeReportExpressionHelpers.Column(orderBy), count, descending);

    public ForgeReportBuilder<T> Pivot(
        string rowExpression,
        string columnExpression,
        string valueExpression,
        string aggregate = "SUM",
        string alias = "PivotValue")
    {
        _definition.Pivot = new ForgeReportPivot
        {
            RowExpression = rowExpression,
            ColumnExpression = columnExpression,
            ValueExpression = valueExpression,
            Aggregate = aggregate,
            Alias = alias
        };

        AddDimensionIfMissing("PivotRow", rowExpression);
        AddDimensionIfMissing("PivotColumn", columnExpression);
        return this;
    }

    public ForgeReportBuilder<T> Pivot<TRow, TColumn, TValue>(
        Expression<Func<T, TRow>> row,
        Expression<Func<T, TColumn>> column,
        Expression<Func<T, TValue>> value,
        string aggregate = "SUM",
        string alias = "PivotValue")
        => Pivot(
            ForgeReportExpressionHelpers.Column(row),
            ForgeReportExpressionHelpers.Column(column),
            ForgeReportExpressionHelpers.Column(value),
            aggregate,
            alias);

    public ForgeReportBuilder<T> PivotByYear<TDate, TColumn, TValue>(
        Expression<Func<T, TDate>> date,
        Expression<Func<T, TColumn>> column,
        Expression<Func<T, TValue>> value,
        string aggregate = "SUM",
        string alias = "PivotValue")
        => Pivot(
            ForgeReportExpressionHelpers.Year(date),
            ForgeReportExpressionHelpers.Column(column),
            ForgeReportExpressionHelpers.Column(value),
            aggregate,
            alias);

    public ForgeReportBuilder<T> Unpivot(string nameColumn, string valueColumn, params string[] sourceColumns)
    {
        var unpivot = new ForgeReportUnpivot
        {
            NameColumn = nameColumn,
            ValueColumn = valueColumn
        };

        foreach (var sourceColumn in sourceColumns.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            unpivot.SourceColumns.Add(sourceColumn);
        }

        _definition.Unpivot = unpivot;
        return this;
    }

    public ForgeReportBuilder<T> Unpivot(string nameColumn, string valueColumn, params Expression<Func<T, object?>>[] sourceColumns)
    {
        var columns = sourceColumns.Select(x => ForgeReportExpressionHelpers.Column(x)).ToArray();
        return Unpivot(nameColumn, valueColumn, columns);
    }

    public ForgeReportBuilder<T> Window(
        string function,
        string? expression,
        string alias,
        IEnumerable<string>? partitionBy = null,
        IEnumerable<string>? orderBy = null,
        string? frameClause = null)
    {
        var window = new ForgeReportWindow
        {
            Function = function,
            Expression = expression,
            Alias = alias,
            FrameClause = frameClause
        };

        if (partitionBy is not null)
        {
            window.PartitionBy.AddRange(partitionBy.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        if (orderBy is not null)
        {
            window.OrderBy.AddRange(orderBy.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        _definition.Windows.Add(window);
        return this;
    }

    public ForgeReportBuilder<T> Window<TValue>(
        string function,
        Expression<Func<T, TValue>> expression,
        string alias,
        IEnumerable<string>? partitionBy = null,
        IEnumerable<string>? orderBy = null,
        string? frameClause = null)
        => Window(function, ForgeReportExpressionHelpers.Column(expression), alias, partitionBy, orderBy, frameClause);

    public ForgeReportBuilder<T> RowNumber(string alias, IEnumerable<string>? partitionBy = null, IEnumerable<string>? orderBy = null)
        => Window("ROW_NUMBER", null, alias, partitionBy, orderBy);

    public ForgeReportBuilder<T> RowNumber<TPartition, TOrder>(
        string alias,
        Expression<Func<T, TPartition>> partitionBy,
        Expression<Func<T, TOrder>> orderBy,
        bool descending = false)
        => RowNumber(
            alias,
            [ForgeReportExpressionHelpers.Column(partitionBy)],
            [ForgeReportExpressionHelpers.Column(orderBy) + (descending ? " DESC" : " ASC")]);

    public ForgeReportBuilder<T> Percentile(string expression, decimal percentile, string alias, IEnumerable<string>? partitionBy = null)
        => Window($"PERCENTILE_CONT({percentile}) WITHIN GROUP (ORDER BY {expression})", null, alias, partitionBy);

    public ForgeReportBuilder<T> Percentile<TValue, TPartition>(
        Expression<Func<T, TValue>> expression,
        decimal percentile,
        string alias,
        Expression<Func<T, TPartition>>? partitionBy = null)
        => Percentile(
            ForgeReportExpressionHelpers.Column(expression),
            percentile,
            alias,
            partitionBy is null ? null : [ForgeReportExpressionHelpers.Column(partitionBy)]);

    public ForgeReportBuilder<T> RollingAverage(string expression, string orderBy, int precedingRows, string alias, IEnumerable<string>? partitionBy = null)
        => Window("AVG", expression, alias, partitionBy, [orderBy], $"ROWS BETWEEN {precedingRows} PRECEDING AND CURRENT ROW");

    public ForgeReportBuilder<T> RollingAverage<TValue, TOrder, TPartition>(
        Expression<Func<T, TValue>> expression,
        Expression<Func<T, TOrder>> orderBy,
        int precedingRows,
        string alias,
        Expression<Func<T, TPartition>>? partitionBy = null)
        => RollingAverage(
            ForgeReportExpressionHelpers.Column(expression),
            ForgeReportExpressionHelpers.Column(orderBy),
            precedingRows,
            alias,
            partitionBy is null ? null : [ForgeReportExpressionHelpers.Column(partitionBy)]);

    public ForgeReportBuilder<T> DrillDown(string name, string expression)
    {
        _definition.DrillDowns.Add(new ForgeReportDrillDown { Name = name, Expression = expression });
        return this;
    }

    public ForgeReportBuilder<T> DrillDown<TValue>(string name, Expression<Func<T, TValue>> expression)
        => DrillDown(name, ForgeReportExpressionHelpers.Column(expression));

    public ForgeReportBuilder<T> DrillThrough(string name, string sql, object? parameters = null)
    {
        _definition.DrillThroughs.Add(new ForgeReportDrillThrough { Name = name, Sql = sql, Parameters = parameters });
        return this;
    }

    public ForgeReportDefinition Build() => _definition;

    public string ToSql() => ForgeReportSqlRenderer.Render(_definition);

    /// <summary>
    /// Backward compatible name for callers that used ToDictionaryProjection() to get SQL.
    /// </summary>
    public string ToDictionaryProjection() => ToSql();

    /// <summary>
    /// Executes the report and returns dynamic dictionary rows. Use this for pivots, unpivots,
    /// windows, aggregates, and reports that do not map to an entity.
    /// </summary>
    public Task<IReadOnlyList<Dictionary<string, object?>>> ToListAsync(CancellationToken cancellationToken = default)
        => _db.QueryDictionaryAsync(ToSql(), _definition.Parameters, cancellationToken: cancellationToken);

    public Task<IReadOnlyList<Dictionary<string, object?>>> ToDictionaryProjectionAsync(CancellationToken cancellationToken = default)
        => ToListAsync(cancellationToken);

    public Task<IReadOnlyList<Dictionary<string, object?>>> ToDictionaryListAsync(CancellationToken cancellationToken = default)
        => ToListAsync(cancellationToken);

    public Task<IReadOnlyList<TResult>> ToListAsync<TResult>(CancellationToken cancellationToken = default)
        => _db.QueryAsync<TResult>(ToSql(), _definition.Parameters, cancellationToken: cancellationToken);

    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var rows = await ToListAsync(cancellationToken);
        return ForgeReportExport.ToCsvBytes(rows);
    }

    public async Task<byte[]> ExportExcelAsync(string worksheetName = "Report", CancellationToken cancellationToken = default)
    {
        var rows = await ToListAsync(cancellationToken);
        return ForgeReportExport.ToExcelXmlBytes(rows, worksheetName);
    }

    private void AddDimensionIfMissing(string name, string expression)
    {
        if (_definition.Dimensions.Any(x => x.Expression.Equals(expression, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _definition.Dimensions.Add(new ForgeReportDimension { Name = name, Expression = expression });
    }
}
