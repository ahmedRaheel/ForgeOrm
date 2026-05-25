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
