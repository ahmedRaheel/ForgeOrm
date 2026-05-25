using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

internal static class ForgeExpressionSqlBuilder
{
    public static string Build(Expression expression, Func<object?, string> addParameter)
    {
        if (expression is BinaryExpression binary)
        {
            var left = Build(binary.Left, addParameter);
            var right = Build(binary.Right, addParameter);

            return binary.NodeType switch
            {
                ExpressionType.Equal => $"{left} = {right}",
                ExpressionType.NotEqual => $"{left} <> {right}",
                ExpressionType.GreaterThan => $"{left} > {right}",
                ExpressionType.GreaterThanOrEqual => $"{left} >= {right}",
                ExpressionType.LessThan => $"{left} < {right}",
                ExpressionType.LessThanOrEqual => $"{left} <= {right}",
                ExpressionType.AndAlso => $"({left} AND {right})",
                ExpressionType.OrElse => $"({left} OR {right})",
                _ => throw new NotSupportedException($"Expression '{binary.NodeType}' is not supported.")
            };
        }

        if (expression is MemberExpression member)
        {
            if (member.Expression?.NodeType == ExpressionType.Parameter && member.Member is PropertyInfo property)
            {
                return ForgeEntityShape.ColumnName(property);
            }

            return addParameter(Evaluate(expression));
        }

        if (expression is ConstantExpression constant)
        {
            return addParameter(constant.Value);
        }

        if (expression is UnaryExpression unary &&
            (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
        {
            return Build(unary.Operand, addParameter);
        }

        throw new NotSupportedException($"Expression '{expression.NodeType}' is not supported.");
    }

    private static object? Evaluate(Expression expression)
    {
        var converted = Expression.Convert(expression, typeof(object));
        return ForgeExpressionDelegateCache.Evaluate(converted);
    }
}
