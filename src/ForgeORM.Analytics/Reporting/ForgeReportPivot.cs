using System.Text;

namespace ForgeORM.Analytics.Reporting;

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
