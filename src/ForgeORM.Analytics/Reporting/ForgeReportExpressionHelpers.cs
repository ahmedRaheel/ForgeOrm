using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Analytics.Reporting;

/// <summary>
/// Expression helpers used by ForgeORM reporting to keep SQL and expression APIs side by side.
/// </summary>
internal static class ForgeReportExpressionHelpers
{
    public static string Column<T, TValue>(Expression<Func<T, TValue>> expression)
        => Column(expression.Body);

    public static string Column(Expression expression)
    {
        while (expression is UnaryExpression unary &&
               (unary.NodeType == ExpressionType.Convert ||
                unary.NodeType == ExpressionType.ConvertChecked ||
                unary.NodeType == ExpressionType.TypeAs))
        {
            expression = unary.Operand;
        }

        if (expression is MemberExpression { Member: PropertyInfo property })
        {
            if (property.Name.Equals(nameof(DateTime.Year), StringComparison.OrdinalIgnoreCase) &&
                property.DeclaringType == typeof(DateTime))
            {
                return $"YEAR({Column(propertyExpression: (MemberExpression)expression!)})";
            }

            if (property.Name.Equals(nameof(DateTime.Month), StringComparison.OrdinalIgnoreCase) &&
                property.DeclaringType == typeof(DateTime))
            {
                return $"MONTH({Column(propertyExpression: (MemberExpression)expression!)})";
            }

            if (property.Name.Equals(nameof(DateTime.Day), StringComparison.OrdinalIgnoreCase) &&
                property.DeclaringType == typeof(DateTime))
            {
                return $"DAY({Column(propertyExpression: (MemberExpression)expression!)})";
            }

            if (property.Name.Equals(nameof(DateTimeOffset.Year), StringComparison.OrdinalIgnoreCase) &&
                property.DeclaringType == typeof(DateTimeOffset))
            {
                return $"YEAR({Column(propertyExpression: (MemberExpression)expression!)})";
            }

            if (property.Name.Equals(nameof(DateTimeOffset.Month), StringComparison.OrdinalIgnoreCase) &&
                property.DeclaringType == typeof(DateTimeOffset))
            {
                return $"MONTH({Column(propertyExpression: (MemberExpression)expression!)})";
            }

            if (property.Name.Equals(nameof(DateTimeOffset.Day), StringComparison.OrdinalIgnoreCase) &&
                property.DeclaringType == typeof(DateTimeOffset))
            {
                return $"DAY({Column(propertyExpression: (MemberExpression)expression!)})";
            }

            return property.Name;
        }

        throw new NotSupportedException($"Reporting expression '{expression.NodeType}' is not supported. Use the SQL-string overload for complex provider-specific expressions.");
    }

    private static string Column(MemberExpression propertyExpression)
    {
        if (propertyExpression.Member is PropertyInfo property)
        {
            return property.Name;
        }

        throw new NotSupportedException("Expression must point to a property.");
    }

    public static string Year<T, TValue>(Expression<Func<T, TValue>> expression)
        => $"YEAR({Column(expression)})";

    public static string Month<T, TValue>(Expression<Func<T, TValue>> expression)
        => $"MONTH({Column(expression)})";

    public static string Day<T, TValue>(Expression<Func<T, TValue>> expression)
        => $"DAY({Column(expression)})";
}
