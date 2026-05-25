using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ForgeORM.Core;
using ForgeORM.QueryAst;

namespace ForgeORM.Core.Search;

internal static class ForgeSearchExpression
{
    public static ForgeSearchExpressionResult Translate<T>(
        Expression<Func<T, bool>> expression,
        int startIndex)
    {
        var parameters = new Dictionary<string, object?>();
        var sql = TranslateNode(
            expression.Body,
            parameters,
            ref startIndex);

        return new ForgeSearchExpressionResult(
            sql,
            parameters);
    }

    public static string Column<T, TValue>(
        Expression<Func<T, TValue>> expression)
    {
        return Column(expression.Body);
    }

    public static string Column(Expression expression)
    {
        while (expression is UnaryExpression unary)
        {
            expression = unary.Operand;
        }

        return expression is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Expression must point to a property.");
    }

    private static string TranslateNode(
        Expression expression,
        Dictionary<string, object?> parameters,
        ref int index)
    {
        if (expression is BinaryExpression binary)
        {
            if (binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                var left = TranslateNode(binary.Left, parameters, ref index);
                var right = TranslateNode(binary.Right, parameters, ref index);
                var op = binary.NodeType == ExpressionType.AndAlso ? "AND" : "OR";

                return $"({left}) {op} ({right})";
            }

            var member = Column(binary.Left);
            var parameter = AddParameter(
                Evaluate(binary.Right),
                parameters,
                ref index);

            return $"{member} {Operator(binary.NodeType)} {parameter}";
        }

        throw new NotSupportedException("Only simple search expressions are supported.");
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
            _ => throw new NotSupportedException($"Operator {type} is not supported.")
        };
    }

    private static string AddParameter(
        object? value,
        Dictionary<string, object?> parameters,
        ref int index)
    {
        var name = "s" + index++;
        parameters[name] = value;
        return "@" + name;
    }

    private static object? Evaluate(Expression expression)
    {
        return ForgeExpressionDelegateCache.Evaluate(expression);
    }
}
