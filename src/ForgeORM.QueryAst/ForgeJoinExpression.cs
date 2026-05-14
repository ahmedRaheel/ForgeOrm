using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

internal sealed partial class ForgeAstSelectBuilder<T>
{
    internal static class ForgeJoinExpression
{
    public static string Translate<TLeft, TRight>(
        Expression<Func<TLeft, TRight, bool>> expression)
    {
        if (expression.Body is not BinaryExpression binary)
            throw new NotSupportedException("Only simple join conditions are supported.");

        var left = Member(binary.Left);
        var right = Member(binary.Right);
        var op = Operator(binary.NodeType);

        return $"{left} {op} {right}";
    }

    private static string Member(Expression expression)
    {
        expression = StripConvert(expression);

        if (expression is MemberExpression member)
        {
            if (member.Expression is ParameterExpression parameter)
                return $"{parameter.Name}.{member.Member.Name}";

            return member.Member.Name;
        }

        throw new NotSupportedException($"Join condition must use member expressions. Found: {expression.NodeType} - {expression}");
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression unary &&
               (unary.NodeType == ExpressionType.Convert ||
                unary.NodeType == ExpressionType.ConvertChecked ||
                unary.NodeType == ExpressionType.TypeAs))
        {
            expression = unary.Operand;
        }

        return expression;
    }

    private static string Operator(ExpressionType type)
    {
        return type switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Join operator {type} is not supported.")
        };
    }
}
}
