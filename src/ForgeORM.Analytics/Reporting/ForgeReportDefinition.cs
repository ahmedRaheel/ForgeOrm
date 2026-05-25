using System.Text;

namespace ForgeORM.Analytics.Reporting;

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
