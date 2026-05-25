using System.Text;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Represents a drill-through query for detail navigation.
/// </summary>
public sealed class ForgeReportDrillThrough
{
    public string Name { get; set; } = string.Empty;

    public string Sql { get; set; } = string.Empty;

    public object? Parameters { get; set; }
}
