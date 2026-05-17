using System.Text;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Supported aggregate functions for report measures.
/// </summary>
public enum ForgeReportAggregate
{
    Count,
    Sum,
    Average,
    Min,
    Max,
    Median,
    Custom
}

/// <summary>
/// Supported window functions for report queries.
/// </summary>
public enum ForgeReportWindowFunction
{
    RowNumber,
    Rank,
    DenseRank,
    Count,
    Sum,
    Average,
    Min,
    Max,
    Lag,
    Lead,
    PercentRank,
    CumeDist,
    PercentileCont,
    PercentileDisc,
    RollingAverage,
    Custom
}

/// <summary>
/// Represents a report dimension used in SELECT and GROUP BY.
/// </summary>
public sealed class ForgeReportDimension
{
    public string Name { get; set; } = string.Empty;

    public string Expression { get; set; } = string.Empty;
}

/// <summary>
/// Represents a report measure/metric.
/// </summary>
public sealed class ForgeReportMeasure
{
    public string Name { get; set; } = string.Empty;

    public string Expression { get; set; } = string.Empty;

    public string Aggregate { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public static ForgeReportMeasure Sum(string expression, string alias) => Create("SUM", expression, alias);

    public static ForgeReportMeasure Count(string expression, string alias) => Create("COUNT", expression, alias);

    public static ForgeReportMeasure Avg(string expression, string alias) => Create("AVG", expression, alias);

    public static ForgeReportMeasure Average(string expression, string alias) => Avg(expression, alias);

    public static ForgeReportMeasure Min(string expression, string alias) => Create("MIN", expression, alias);

    public static ForgeReportMeasure Max(string expression, string alias) => Create("MAX", expression, alias);

    public static ForgeReportMeasure Custom(string aggregate, string expression, string alias) => Create(aggregate, expression, alias);

    private static ForgeReportMeasure Create(string aggregate, string expression, string alias)
    {
        return new ForgeReportMeasure
        {
            Name = alias,
            Expression = expression,
            Aggregate = aggregate,
            Alias = alias
        };
    }
}

/// <summary>
/// Represents report pivot configuration.
/// </summary>
public sealed class ForgeReportPivot
{
    public string RowExpression { get; set; } = string.Empty;

    public string ColumnExpression { get; set; } = string.Empty;

    public string ValueExpression { get; set; } = string.Empty;

    public string Aggregate { get; set; } = "SUM";

    public string Alias { get; set; } = "PivotValue";
}

/// <summary>
/// Represents report unpivot configuration.
/// </summary>
public sealed class ForgeReportUnpivot
{
    public string NameColumn { get; set; } = "MetricName";

    public string ValueColumn { get; set; } = "MetricValue";

    public List<string> SourceColumns { get; } = [];
}

/// <summary>
/// Represents a SQL window expression.
/// </summary>
public sealed class ForgeReportWindow
{
    public string Function { get; set; } = string.Empty;

    public string? Expression { get; set; }

    public List<string> PartitionBy { get; } = [];

    public List<string> OrderBy { get; } = [];

    public string? FrameClause { get; set; }

    public string Alias { get; set; } = string.Empty;
}

/// <summary>
/// Represents drill-down metadata for report UI navigation.
/// </summary>
public sealed class ForgeReportDrillDown
{
    public string Name { get; set; } = string.Empty;

    public string Expression { get; set; } = string.Empty;
}

/// <summary>
/// Represents a drill-through query for detail navigation.
/// </summary>
public sealed class ForgeReportDrillThrough
{
    public string Name { get; set; } = string.Empty;

    public string Sql { get; set; } = string.Empty;

    public object? Parameters { get; set; }
}

/// <summary>
/// Complete report definition.
/// </summary>
public sealed class ForgeReportDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Table { get; set; } = string.Empty;

    public List<ForgeReportDimension> Dimensions { get; } = [];

    public List<ForgeReportMeasure> Measures { get; } = [];

    public List<ForgeReportWindow> Windows { get; } = [];

    public List<ForgeReportDrillDown> DrillDowns { get; } = [];

    public List<ForgeReportDrillThrough> DrillThroughs { get; } = [];

    public ForgeReportPivot? Pivot { get; set; }

    public ForgeReportUnpivot? Unpivot { get; set; }

    public string? WhereSql { get; set; }

    public object? Parameters { get; set; }

    public string? OrderBySql { get; set; }

    public int? Top { get; set; }
}

/// <summary>
/// Export helpers for report rows.
/// </summary>
public static class ForgeReportExport
{
    public static byte[] ToCsvBytes(IEnumerable<IDictionary<string, object?>> rows)
    {
        var csv = ToCsvText(rows);
        return Encoding.UTF8.GetBytes(csv);
    }

    public static string ToCsvText(IEnumerable<IDictionary<string, object?>> rows)
    {
        var list = rows.ToList();
        if (list.Count == 0)
        {
            return string.Empty;
        }

        var columns = list.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', columns.Select(EscapeCsv)));

        foreach (var row in list)
        {
            sb.AppendLine(string.Join(',', columns.Select(column =>
            {
                row.TryGetValue(column, out var value);
                return EscapeCsv(value?.ToString() ?? string.Empty);
            })));
        }

        return sb.ToString();
    }

    public static byte[] ToExcelXmlBytes(IEnumerable<IDictionary<string, object?>> rows, string worksheetName = "Report")
    {
        var list = rows.ToList();
        var columns = list.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        sb.AppendLine($"<Worksheet ss:Name=\"{EscapeXml(worksheetName)}\"><Table>");
        sb.AppendLine("<Row>" + string.Join(string.Empty, columns.Select(c => Cell(c))) + "</Row>");

        foreach (var row in list)
        {
            sb.AppendLine("<Row>" + string.Join(string.Empty, columns.Select(column =>
            {
                row.TryGetValue(column, out var value);
                return Cell(value?.ToString() ?? string.Empty);
            })) + "</Row>");
        }

        sb.AppendLine("</Table></Worksheet></Workbook>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Cell(string value) => $"<Cell><Data ss:Type=\"String\">{EscapeXml(value)}</Data></Cell>";

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
