using System.Linq.Expressions;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Inference-friendly reporting overloads.
/// 
/// Goal:
/// db.Report<Order>() already knows TEntity, so users should not repeat generic arguments.
/// </summary>
public static class ForgeReportInferenceFriendlyExtensions
{
    public static ForgeReportBuilder<TEntity> Dimension<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        string alias,
        Expression<Func<TEntity, TValue>> expression)
    {
        return report.Dimension(alias, ToSqlExpression(expression.Body));
    }

    public static ForgeReportBuilder<TEntity> Dimension<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> expression)
    {
        var column = ToSqlExpression(expression.Body);
        return report.Dimension(column, column);
    }

    public static ForgeReportBuilder<TEntity> DimensionExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        string alias,
        Expression<Func<TEntity, TValue>> expression)
    {
        return report.Dimension(alias, expression);
    }

    public static ForgeReportBuilder<TEntity> SumExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Sum(
                ToSqlExpression(expression.Body),
                alias));
    }

    public static ForgeReportBuilder<TEntity> SumSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string sqlExpression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Sum(sqlExpression, alias));
    }

    public static ForgeReportBuilder<TEntity> AvgExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Avg(
                ToSqlExpression(expression.Body),
                alias));
    }

    public static ForgeReportBuilder<TEntity> MinExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Min(
                ToSqlExpression(expression.Body),
                alias));
    }

    public static ForgeReportBuilder<TEntity> MaxExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Max(
                ToSqlExpression(expression.Body),
                alias));
    }

    public static ForgeReportBuilder<TEntity> CountExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias = "Total")
    {
        return report.Measure(
            ForgeReportMeasure.Count(
                ToSqlExpression(expression.Body),
                alias));
    }

    public static ForgeReportBuilder<TEntity> TopNSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string orderBy,
        int count,
        bool descending = true)
    {
        return report.TopN(orderBy, count, descending);
    }

    public static ForgeReportBuilder<TEntity> TopNExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> orderBy,
        int count,
        bool descending = true)
    {
        return report.TopN(ToSqlExpression(orderBy.Body), count, descending);
    }

    public static ForgeReportBuilder<TEntity> PivotExpr<TEntity, TRow, TColumn, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TRow>> row,
        Expression<Func<TEntity, TColumn>> column,
        Expression<Func<TEntity, TValue>> value,
        string aggregate = "SUM",
        string alias = "PivotValue")
    {
        return report.Pivot(
            rowExpression: ToSqlExpression(row.Body),
            columnExpression: ToSqlExpression(column.Body),
            valueExpression: ToSqlExpression(value.Body),
            aggregate: aggregate,
            alias: alias);
    }

    public static ForgeReportBuilder<TEntity> PivotSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string row,
        string column,
        string value,
        string aggregate = "SUM",
        string alias = "PivotValue")
    {
        return report.Pivot(
            rowExpression: row,
            columnExpression: column,
            valueExpression: value,
            aggregate: aggregate,
            alias: alias);
    }

    private static string ToSqlExpression(Expression expression)
    {
        while (expression is UnaryExpression unary)
        {
            expression = unary.Operand;
        }

        if (expression is MemberExpression member)
        {
            if (member.Member.Name == nameof(DateTime.Year) &&
                member.Expression is MemberExpression innerDate)
            {
                return $"YEAR({innerDate.Member.Name})";
            }

            if (member.Member.Name == nameof(DateTime.Month) &&
                member.Expression is MemberExpression innerMonth)
            {
                return $"MONTH({innerMonth.Member.Name})";
            }

            if (member.Member.Name == nameof(DateTime.Day) &&
                member.Expression is MemberExpression innerDay)
            {
                return $"DAY({innerDay.Member.Name})";
            }

            return member.Member.Name;
        }

        throw new NotSupportedException(
            $"Expression '{expression}' is not supported. Use a simple property expression or SQL overload.");
    }
}
