using System.Linq.Expressions;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Compatibility overloads for the preferred simple reporting syntax.
/// These overloads intentionally support calls such as:
/// .Dimension<Order>("CustomerId", x => x.Id)
/// .Sum<Order, decimal>(x => x.GrandTotal, "Revenue")
/// .Pivot(row: "...", column: "...", value: "...")
/// </summary>
public static class ForgeReportFriendlyOverloads
{
    /// <summary>
    /// Expression dimension overload with entity generic parameter.
    /// Allows: .Dimension&lt;Order&gt;("CustomerId", x =&gt; x.Id)
    /// </summary>
    public static ForgeReportBuilder<TEntity> Dimension<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string name,
        Expression<Func<TEntity, object?>> expression)
    {
        return report.Dimension(name, ToSqlExpression(expression.Body));
    }

    /// <summary>
    /// Expression dimension overload with explicit value type.
    /// Allows: .Dimension&lt;Order, int&gt;("CustomerId", x =&gt; x.Id)
    /// </summary>
    public static ForgeReportBuilder<TEntity> Dimension<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        string name,
        Expression<Func<TEntity, TValue>> expression)
    {
        return report.Dimension(name, ToSqlExpression(expression.Body));
    }

    /// <summary>
    /// Alias for expression dimension.
    /// </summary>
    public static ForgeReportBuilder<TEntity> DimensionExpr<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string name,
        Expression<Func<TEntity, object?>> expression)
    {
        return report.Dimension(name, expression);
    }

    /// <summary>
    /// Expression SUM overload with entity + value generic parameters.
    /// Allows: .Sum&lt;Order, decimal&gt;(x =&gt; x.GrandTotal, "Revenue")
    /// </summary>
    public static ForgeReportBuilder<TEntity> Sum<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Sum(
                ToSqlExpression(expression.Body),
                alias));
    }

    public static ForgeReportBuilder<TEntity> Avg<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Avg(
                ToSqlExpression(expression.Body),
                alias));
    }

    public static ForgeReportBuilder<TEntity> Average<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Avg(expression, alias);
    }

    public static ForgeReportBuilder<TEntity> Min<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Min(
                ToSqlExpression(expression.Body),
                alias));
    }

    public static ForgeReportBuilder<TEntity> Max<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
    {
        return report.Measure(
            ForgeReportMeasure.Max(
                ToSqlExpression(expression.Body),
                alias));
    }

    /// <summary>
    /// Friendly SQL pivot overload with parameter names row/column/value.
    /// Allows named arguments:
    /// .Pivot(row: "YEAR(CreatedAt)", column: "Status", value: "GrandTotal", ...)
    /// </summary>
    public static ForgeReportBuilder<TEntity> Pivot<TEntity>(
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

    /// <summary>
    /// Friendly expression pivot overload.
    /// Allows:
    /// .Pivot&lt;Order, int, OrderStatus, decimal&gt;(x =&gt; x.CreatedAt.Year, x =&gt; x.Status, x =&gt; x.GrandTotal)
    /// </summary>
    public static ForgeReportBuilder<TEntity> Pivot<TEntity, TRow, TColumn, TValue>(
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

    /// <summary>
    /// Alias for SQL TopN.
    /// </summary>
    public static ForgeReportBuilder<TEntity> TopNSql<TEntity>(
        this ForgeReportBuilder<TEntity> report,
        string orderBy,
        int count,
        bool descending = true)
    {
        return report.TopN(orderBy, count, descending);
    }

    /// <summary>
    /// Expression TopN with explicit entity generic.
    /// </summary>
    public static ForgeReportBuilder<TEntity> TopN<TEntity, TValue>(
        this ForgeReportBuilder<TEntity> report,
        Expression<Func<TEntity, TValue>> orderBy,
        int count,
        bool descending = true)
    {
        return report.TopN(ToSqlExpression(orderBy.Body), count, descending);
    }

    private static string ToSqlExpression(Expression expression)
    {
        while (expression is UnaryExpression unary)
        {
            expression = unary.Operand;
        }

        if (expression is MemberExpression member)
        {
            // x.CreatedAt.Year is represented as MemberExpression Year over MemberExpression CreatedAt
            if (member.Member.Name == nameof(DateTime.Year) &&
                member.Expression is MemberExpression inner)
            {
                return $"YEAR({inner.Member.Name})";
            }

            return member.Member.Name;
        }

        if (expression is MethodCallExpression call &&
            call.Method.Name == "get_Year" &&
            call.Object is MemberExpression yearMember)
        {
            return $"YEAR({yearMember.Member.Name})";
        }

        throw new NotSupportedException(
            $"Expression '{expression}' is not supported. Use a simple property like x => x.Id, x => x.CreatedAt.Year, or SQL overload such as Dimension(\"Year\", \"YEAR(CreatedAt)\").");
    }
}
