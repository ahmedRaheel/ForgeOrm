using System.Text;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Represents report unpivot configuration.
/// </summary>
public sealed class ForgeReportUnpivot
{
    public string NameColumn { get; set; } = "MetricName";

    public string ValueColumn { get; set; } = "MetricValue";

    public List<string> SourceColumns { get; } = [];
}
