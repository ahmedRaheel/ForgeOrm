using System.Text;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Represents drill-down metadata for report UI navigation.
/// </summary>
public sealed class ForgeReportDrillDown
{
    public string Name { get; set; } = string.Empty;

    public string Expression { get; set; } = string.Empty;
}
