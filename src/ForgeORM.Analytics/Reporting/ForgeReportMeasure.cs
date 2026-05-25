using System.Text;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Represents a report measure/metric.
/// </summary>
public sealed class ForgeReportMeasure
{
    public string Name { get; set; } = string.Empty;

    public string Expression { get; set; } = string.Empty;

    public string Aggregate { get; set; } = string.Empty;

    public string Alias { get; set; } = string.Empty;

    public static ForgeReportMeasure Sum(string expression, string alias) => Create("SUM", expression, alias);

    public static ForgeReportMeasure Count(string expression, string alias) => Create("COUNT", expression, alias);

    public static ForgeReportMeasure Avg(string expression, string alias) => Create("AVG", expression, alias);

    public static ForgeReportMeasure Average(string expression, string alias) => Avg(expression, alias);

    public static ForgeReportMeasure Min(string expression, string alias) => Create("MIN", expression, alias);

    public static ForgeReportMeasure Max(string expression, string alias) => Create("MAX", expression, alias);

    public static ForgeReportMeasure Custom(string aggregate, string expression, string alias) => Create(aggregate, expression, alias);

    private static ForgeReportMeasure Create(string aggregate, string expression, string alias)
    {
        return new ForgeReportMeasure
        {
            Name = alias,
            Expression = expression,
            Aggregate = aggregate,
            Alias = alias
        };
    }
}
