using System.Text;

namespace ForgeORM.Analytics.Reporting;

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
