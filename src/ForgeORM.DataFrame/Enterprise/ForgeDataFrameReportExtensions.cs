using ForgeORM.DataFrame;

namespace ForgeORM.DataFrame.Enterprise;

/// <summary>
/// Report builder extensions for ForgeDataFrame.
/// </summary>
public static class ForgeDataFrameReportExtensions
{
    /// <summary>
    /// Starts a report builder.
    /// </summary>
    public static ForgeReportBuilder Report(this ForgeDataFrame frame) => new(frame);
}
