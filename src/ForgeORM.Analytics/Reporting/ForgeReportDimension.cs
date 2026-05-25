using System.Text;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Represents a report dimension used in SELECT and GROUP BY.
/// </summary>
public sealed class ForgeReportDimension
{
    public string Name { get; set; } = string.Empty;

    public string Expression { get; set; } = string.Empty;
}
