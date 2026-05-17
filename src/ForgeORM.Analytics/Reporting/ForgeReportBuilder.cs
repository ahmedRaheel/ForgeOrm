using ForgeORM.Core;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Fluent report builder for enterprise reporting, pivots, drill-down, and exports.
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

    public ForgeReportBuilder<T> Measure(ForgeReportMeasure measure)
    {
        _definition.Measures.Add(measure);
        return this;
    }

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

    public ForgeReportBuilder<T> TopN(string orderBySql, int count, bool descending = true)
    {
        _definition.Top = count;
        _definition.OrderBySql = orderBySql + (descending ? " DESC" : " ASC");
        return this;
    }

    public ForgeReportBuilder<T> Pivot(string rowExpression, string columnExpression, string valueExpression, string aggregate = "SUM", string alias = "PivotValue")
    {
        _definition.Pivot = new ForgeReportPivot
        {
            RowExpression = rowExpression,
            ColumnExpression = columnExpression,
            ValueExpression = valueExpression,
            Aggregate = aggregate,
            Alias = alias
        };

        _definition.Dimensions.Add(new ForgeReportDimension { Name = "PivotRow", Expression = rowExpression });
        _definition.Dimensions.Add(new ForgeReportDimension { Name = "PivotColumn", Expression = columnExpression });
        return this;
    }

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

    public ForgeReportBuilder<T> RowNumber(string alias, IEnumerable<string>? partitionBy = null, IEnumerable<string>? orderBy = null)
        => Window("ROW_NUMBER", null, alias, partitionBy, orderBy);

    public ForgeReportBuilder<T> Percentile(string expression, decimal percentile, string alias, IEnumerable<string>? partitionBy = null)
        => Window($"PERCENTILE_CONT({percentile}) WITHIN GROUP (ORDER BY {expression})", null, alias, partitionBy);

    public ForgeReportBuilder<T> RollingAverage(string expression, string orderBy, int precedingRows, string alias, IEnumerable<string>? partitionBy = null)
        => Window("AVG", expression, alias, partitionBy, new[] { orderBy }, $"ROWS BETWEEN {precedingRows} PRECEDING AND CURRENT ROW");

    public ForgeReportBuilder<T> DrillDown(string name, string expression)
    {
        _definition.DrillDowns.Add(new ForgeReportDrillDown { Name = name, Expression = expression });
        return this;
    }

    public ForgeReportBuilder<T> DrillThrough(string name, string sql, object? parameters = null)
    {
        _definition.DrillThroughs.Add(new ForgeReportDrillThrough { Name = name, Sql = sql, Parameters = parameters });
        return this;
    }

    public ForgeReportDefinition Build() => _definition;

    public string ToSql() => ForgeReportSqlRenderer.Render(_definition);

    public Task<IReadOnlyList<TResult>> ToListAsync<TResult>(CancellationToken cancellationToken = default)
        => _db.QueryAsync<TResult>(ToSql(), _definition.Parameters, cancellationToken: cancellationToken);

    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.QueryAsync<Dictionary<string, object?>>(ToSql(), _definition.Parameters, cancellationToken: cancellationToken);
        return ForgeReportExport.ToCsvBytes(rows);
    }

    public async Task<byte[]> ExportExcelAsync(string worksheetName = "Report", CancellationToken cancellationToken = default)
    {
        var rows = await _db.QueryAsync<Dictionary<string, object?>>(ToSql(), _definition.Parameters, cancellationToken: cancellationToken);
        return ForgeReportExport.ToExcelXmlBytes(rows, worksheetName);
    }
}
