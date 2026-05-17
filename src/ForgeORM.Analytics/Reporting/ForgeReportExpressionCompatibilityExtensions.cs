using System.Linq.Expressions;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Single consolidated expression overload set for reporting.
/// Supports both clean inference and explicit generic forms without ambiguity.
/// </summary>
public static class ForgeReportExpressionCompatibilityExtensions
{
    // Dimension

    public static ForgeReportBuilder<TEntity> DimensionExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> expression)
    {
        var column = ToSqlExpression(expression.Body);
        return report.Dimension(column, column);
    }

    public static ForgeReportBuilder<TEntity> DimensionExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression)
    {
        var column = ToSqlExpression(expression.Body);
        return report.Dimension(column, column);
    }

    public static ForgeReportBuilder<TEntity> DimensionExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string alias,
        Expression<Func<TEntity, object?>> expression)
    {
        return report.Dimension(alias, ToSqlExpression(expression.Body));
    }

    public static ForgeReportBuilder<TEntity> DimensionExpr<TEntity, TValue>(
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
        return report.DimensionExpr(expression);
    }

    public static ForgeReportBuilder<TEntity> Dimension<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression)
    {
        return report.DimensionExpr(expression);
    }

    public static ForgeReportBuilder<TEntity> Dimension<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string alias,
        Expression<Func<TEntity, object?>> expression)
    {
        return report.DimensionExpr(alias, expression);
    }

    public static ForgeReportBuilder<TEntity> Dimension<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        string alias,
        Expression<Func<TEntity, TValue>> expression)
    {
        return report.DimensionExpr(alias, expression);
    }

    // Aggregates

    public static ForgeReportBuilder<TEntity> SumExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> expression,
        string alias)
    {
        return report.Measure(ForgeReportMeasure.Sum(ToSqlExpression(expression.Body), alias));
    }

    public static ForgeReportBuilder<TEntity> SumExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(ForgeReportMeasure.Sum(ToSqlExpression(expression.Body), alias));
    }

    public static ForgeReportBuilder<TEntity> Sum<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> expression,
        string alias)
    {
        return report.SumExpr(expression, alias);
    }

    public static ForgeReportBuilder<TEntity> Sum<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.SumExpr(expression, alias);
    }

    public static ForgeReportBuilder<TEntity> AvgExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> expression,
        string alias)
    {
        return report.Measure(ForgeReportMeasure.Avg(ToSqlExpression(expression.Body), alias));
    }

    public static ForgeReportBuilder<TEntity> AvgExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(ForgeReportMeasure.Avg(ToSqlExpression(expression.Body), alias));
    }

    public static ForgeReportBuilder<TEntity> AverageExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> expression,
        string alias)
    {
        return report.AvgExpr(expression, alias);
    }

    public static ForgeReportBuilder<TEntity> AverageExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.AvgExpr(expression, alias);
    }

    public static ForgeReportBuilder<TEntity> MinExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> expression,
        string alias)
    {
        return report.Measure(ForgeReportMeasure.Min(ToSqlExpression(expression.Body), alias));
    }

    public static ForgeReportBuilder<TEntity> MinExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(ForgeReportMeasure.Min(ToSqlExpression(expression.Body), alias));
    }

    public static ForgeReportBuilder<TEntity> MaxExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> expression,
        string alias)
    {
        return report.Measure(ForgeReportMeasure.Max(ToSqlExpression(expression.Body), alias));
    }

    public static ForgeReportBuilder<TEntity> MaxExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(ForgeReportMeasure.Max(ToSqlExpression(expression.Body), alias));
    }

    public static ForgeReportBuilder<TEntity> CountExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> expression,
        string alias = "Total")
    {
        return report.Measure(ForgeReportMeasure.Count(ToSqlExpression(expression.Body), alias));
    }

    public static ForgeReportBuilder<TEntity> CountExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias = "Total")
    {
        return report.Measure(ForgeReportMeasure.Count(ToSqlExpression(expression.Body), alias));
    }

    // Pivot / TopN

    public static ForgeReportBuilder<TEntity> PivotExpr<TEntity, TRow, TColumn, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TRow>> row,
        Expression<Func<TEntity, TColumn>> column,
        Expression<Func<TEntity, TValue>> value,
        string aggregate = "SUM",
        string alias = "PivotValue")
    {
        return report.Pivot(
            row: ToSqlExpression(row.Body),
            column: ToSqlExpression(column.Body),
            value: ToSqlExpression(value.Body),
            aggregate: aggregate,
            alias: alias);
    }

    public static ForgeReportBuilder<TEntity> TopNExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, object?>> orderBy,
        int take,
        bool descending = true)
    {
        return report.TopN(ToSqlExpression(orderBy.Body), take, descending);
    }

    public static ForgeReportBuilder<TEntity> TopNExpr<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> orderBy,
        int take,
        bool descending = true)
    {
        return report.TopN(ToSqlExpression(orderBy.Body), take, descending);
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
                member.Expression is MemberExpression innerYear)
            {
                return $"YEAR({innerYear.Member.Name})";
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
            $"Expression '{expression}' is not supported. Use simple member expressions like x => x.CustomerId or x => x.CreatedAt.Year.");
    }
}
