namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// SQL/string aliases for the three supported entry styles.
/// Expression overloads live in ForgeReportExpressionCompatibilityExtensions.
/// </summary>
public static class ForgeReportThreeEntryStyles
{
    public static ForgeReportBuilder<TEntity> DimensionSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string alias,
        string sqlExpression)
    {
        return report.Dimension(alias, sqlExpression);
    }

    public static ForgeReportBuilder<TEntity> SumSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string sqlExpression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Sum(sqlExpression, alias));
    }

    public static ForgeReportBuilder<TEntity> AvgSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string sqlExpression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Avg(sqlExpression, alias));
    }

    public static ForgeReportBuilder<TEntity> MinSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string sqlExpression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Min(sqlExpression, alias));
    }

    public static ForgeReportBuilder<TEntity> MaxSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string sqlExpression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Max(sqlExpression, alias));
    }

    public static ForgeReportBuilder<TEntity> PivotSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string row,
        string column,
        string value,
        string aggregate = "SUM",
        string alias = "Value")
    {
        return report.Pivot(
            row: row,
            column: column,
            value: value,
            aggregate: aggregate,
            alias: alias);
    }

    public static ForgeReportBuilder<TEntity> TopNSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string orderBy,
        int take,
        bool descending = true)
    {
        return report.TopN(orderBy, take, descending);
    }
}
