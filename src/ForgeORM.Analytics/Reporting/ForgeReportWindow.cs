using System.Text;

namespace ForgeORM.Analytics.Reporting;

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
