using System.Linq.Expressions;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Reporting aliases that make the three supported entry styles explicit:
/// builder, raw SQL strings, and expressions.
/// </summary>
public static class ForgeReportThreeEntryStyles
{
    /// <summary>
    /// Raw SQL/string-style dimension.
    /// </summary>
    public static TReport DimensionSql<TReport>(
        this TReport report,
        string alias,
        string sqlExpression)
        where TReport : class
    {
        return Invoke<TReport>(report, "Dimension", alias, sqlExpression);
    }

    /// <summary>
    /// Expression-style dimension.
    /// </summary>
    public static TReport DimensionExpr<TReport, TEntity, TValue>(
        this TReport report,
        Expression<Func<TEntity, TValue>> expression)
        where TReport : class
    {
        var name = MemberName(expression.Body);
        return Invoke<TReport>(report, "Dimension", name, name);
    }

    /// <summary>
    /// Raw SQL/string-style sum measure.
    /// </summary>
    public static TReport SumSql<TReport>(
        this TReport report,
        string sqlExpression,
        string alias)
        where TReport : class
    {
        return Invoke<TReport>(
            report,
            "Measure",
            ForgeReportMeasure.Sum(sqlExpression, alias));
    }

    /// <summary>
    /// Expression-style sum measure.
    /// </summary>
    public static TReport SumExpr<TReport, TEntity, TValue>(
        this TReport report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
        where TReport : class
    {
        return Invoke<TReport>(
            report,
            "Measure",
            ForgeReportMeasure.Sum(MemberName(expression.Body), alias));
    }

    /// <summary>
    /// Raw SQL/string-style pivot.
    /// </summary>
    public static TReport PivotSql<TReport>(
        this TReport report,
        string row,
        string column,
        string value,
        string aggregate = "SUM",
        string alias = "Value")
        where TReport : class
    {
        return Invoke<TReport>(report, "Pivot", row, column, value, aggregate, alias);
    }

    /// <summary>
    /// Expression-style pivot.
    /// </summary>
    public static TReport PivotExpr<TReport, TEntity, TRow, TColumn, TValue>(
        this TReport report,
        Expression<Func<TEntity, TRow>> row,
        Expression<Func<TEntity, TColumn>> column,
        Expression<Func<TEntity, TValue>> value,
        string aggregate = "SUM",
        string alias = "Value")
        where TReport : class
    {
        return Invoke<TReport>(
            report,
            "Pivot",
            MemberName(row.Body),
            MemberName(column.Body),
            MemberName(value.Body),
            aggregate,
            alias);
    }

    /// <summary>
    /// Raw SQL/string-style TopN.
    /// </summary>
    public static TReport TopNSql<TReport>(
        this TReport report,
        string orderBy,
        int take)
        where TReport : class
    {
        return Invoke<TReport>(report, "TopN", orderBy, take);
    }

    /// <summary>
    /// Expression-style TopN.
    /// </summary>
    public static TReport TopNExpr<TReport, TEntity, TValue>(
        this TReport report,
        Expression<Func<TEntity, TValue>> orderBy,
        int take)
        where TReport : class
    {
        return Invoke<TReport>(report, "TopN", MemberName(orderBy.Body), take);
    }

    private static TReport Invoke<TReport>(
        TReport report,
        string methodName,
        params object?[] args)
        where TReport : class
    {
        var method = report.GetType()
            .GetMethods()
            .FirstOrDefault(x =>
                x.Name.Equals(methodName, StringComparison.Ordinal) &&
                x.GetParameters().Length == args.Length);

        if (method is null)
        {
            throw new MissingMethodException(report.GetType().Name, methodName);
        }

        return method.Invoke(report, args) as TReport ?? report;
    }

    private static string MemberName(Expression expression)
    {
        while (expression is UnaryExpression unary)
        {
            expression = unary.Operand;
        }

        return expression switch
        {
            MemberExpression member => member.Member.Name,
            MethodCallExpression call when call.Method.Name == "get_Year" && call.Object is MemberExpression member =>
                $"YEAR({member.Member.Name})",
            _ => throw new NotSupportedException("Only simple member expressions are supported.")
        };
    }
}
