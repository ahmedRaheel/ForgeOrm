using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

internal static class ForgeAstExpression
{
    public static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        Expression body = expression.Body;

        if (body is UnaryExpression unary)
            body = unary.Operand;

        return body is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Only simple member expressions are supported.");
    }

    public static string Translate<T>(Expression<Func<T, bool>> expression)
    {
        if (expression.Body is not BinaryExpression binary)
            throw new NotSupportedException("Only simple binary expressions are supported in the AST MVP.");

        return $"{Member(binary.Left)} {Operator(binary.NodeType)} {Value(binary.Right)}";
    }

    private static string Member(Expression expression)
    {
        return expression is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Left side must be a member expression.");
    }

    private static string Operator(ExpressionType type) => type switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "<>",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        _ => throw new NotSupportedException($"Operator {type} is not supported.")
    };

    private static string Value(Expression expression)
    {
        var value = Expression.Lambda(expression).Compile().DynamicInvoke();

        return value switch
        {
            null => "NULL",
            string s => $"'{s.Replace("'", "''")}'",
            DateTime d => $"'{d:yyyy-MM-dd HH:mm:ss}'",
            bool b => b ? "1" : "0",
            _ => value?.ToString() ?? "NULL"
        };
    }
}
