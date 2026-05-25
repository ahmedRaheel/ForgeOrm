using System.Linq.Expressions;
using ForgeORM.Core;
using ForgeORM.Core.Materialization;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Expression overload helpers for reporting. Every SQL-string action should have an expression version.
/// These helpers resolve simple member expressions to column names.
/// </summary>
public static class ForgeReportExpressionExtensions
{
    public static TReport Dimension<TReport, TEntity, TValue>(
        this TReport report,
        Expression<Func<TEntity, TValue>> expression)
        where TReport : class
    {
        var name = GetMemberName(expression.Body);
        return InvokeFluent<TReport>(report, "Dimension", name, name);
    }

    public static TReport Dimension<TReport, TEntity, TValue>(
        this TReport report,
        string alias,
        Expression<Func<TEntity, TValue>> expression)
        where TReport : class
    {
        var name = GetMemberName(expression.Body);
        return InvokeFluent<TReport>(report, "Dimension", alias, name);
    }

    public static TReport Sum<TReport, TEntity, TValue>(
        this TReport report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
        where TReport : class
    {
        var column = GetMemberName(expression.Body);
        var measure = ForgeReportMeasure.Sum(column, alias);
        return InvokeFluent<TReport>(report, "Measure", measure);
    }

    public static TReport Avg<TReport, TEntity, TValue>(
        this TReport report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
        where TReport : class
    {
        var column = GetMemberName(expression.Body);
        var measure = ForgeReportMeasure.Avg(column, alias);
        return InvokeFluent<TReport>(report, "Measure", measure);
    }

    public static TReport Min<TReport, TEntity, TValue>(
        this TReport report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
        where TReport : class
    {
        var column = GetMemberName(expression.Body);
        var measure = ForgeReportMeasure.Min(column, alias);
        return InvokeFluent<TReport>(report, "Measure", measure);
    }

    public static TReport Max<TReport, TEntity, TValue>(
        this TReport report,
        Expression<Func<TEntity, TValue>> expression,
        string alias)
        where TReport : class
    {
        var column = GetMemberName(expression.Body);
        var measure = ForgeReportMeasure.Max(column, alias);
        return InvokeFluent<TReport>(report, "Measure", measure);
    }

    public static TReport Count<TReport>(
        this TReport report,
        string alias = "Count")
        where TReport : class
    {
        var measure = ForgeReportMeasure.Count("*", alias);
        return InvokeFluent<TReport>(report, "Measure", measure);
    }

    public static TReport Pivot<TReport, TEntity, TRow, TColumn, TValue>(
        this TReport report,
        Expression<Func<TEntity, TRow>> row,
        Expression<Func<TEntity, TColumn>> column,
        Expression<Func<TEntity, TValue>> value,
        string aggregate = "SUM",
        string alias = "Value")
        where TReport : class
    {
        return InvokeFluent<TReport>(
            report,
            "Pivot",
            GetMemberName(row.Body),
            GetMemberName(column.Body),
            GetMemberName(value.Body),
            aggregate,
            alias);
    }

    public static TReport TopN<TReport, TEntity, TValue>(
        this TReport report,
        Expression<Func<TEntity, TValue>> orderBy,
        int take)
        where TReport : class
    {
        return InvokeFluent<TReport>(
            report,
            "TopN",
            GetMemberName(orderBy.Body),
            take);
    }

    private static TReport InvokeFluent<TReport>(
        TReport report,
        string methodName,
        params object?[] args)
        where TReport : class
    {
        var method = report.GetType()
            .GetMethods()
            .FirstOrDefault(x =>
                x.Name == methodName &&
                x.GetParameters().Length == args.Length);

        if (method is null)
        {
            throw new MissingMethodException(
                report.GetType().Name,
                $"{methodName}({string.Join(", ", args.Select(x => x?.GetType().Name ?? "null"))})");
        }

        var result = method.Invoke(report, args);
        return result as TReport ?? report;
    }

    private static string GetMemberName(Expression expression)
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
            _ => throw new NotSupportedException($"Expression '{expression}' must be a simple member expression.")
        };
    }
}
