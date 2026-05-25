using System.Linq.Expressions;
using System.Text;
using ForgeORM.QueryAst;

namespace ForgeORM.Core;

internal static class ForgeDbExpressionSql
{
    public static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        var member = GetMemberExpression(expression.Body);
        return member.Member.Name;
    }

    public static Type MemberType<T>(Expression<Func<T, object>> expression)
    {
        var member = GetMemberExpression(expression.Body);
        return member.Type;
    }

    public static string TranslateWhere<T>(Expression<Func<T, bool>> expression)
        => Translate(expression.Body);

    private static string Translate(Expression expression)
    {
        if (expression is BinaryExpression binary)
        {
            if (binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                var op = binary.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({Translate(binary.Left)}) {op} ({Translate(binary.Right)})";
            }

            return $"{MemberSql(binary.Left)} {Operator(binary.NodeType)} {LiteralSql(binary.Right)}";
        }

        if (expression is MethodCallExpression call && call.Method.Name == nameof(string.Contains) && call.Object is not null)
            return $"{MemberSql(call.Object)} LIKE '%' + {LiteralSql(call.Arguments[0])} + '%'";

        throw new NotSupportedException($"Expression '{expression.NodeType}' is not supported for CTE/temp-table SQL generation yet.");
    }

    private static string MemberSql(Expression expression)
    {
        if (expression is UnaryExpression unary) expression = unary.Operand;
        if (expression is MemberExpression member && member.Expression?.NodeType == ExpressionType.Parameter)
            return member.Member.Name;
        throw new NotSupportedException("Left side must be an entity member expression.");
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

    private static string LiteralSql(Expression expression)
    {
        var value = Evaluate(expression);
        return value switch
        {
            null => "NULL",
            string s => "'" + s.Replace("'", "''") + "'",
            DateTime d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss") + "'",
            DateTimeOffset d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss zzz") + "'",
            Guid g => "'" + g + "'",
            bool b => b ? "1" : "0",
            Enum e => Convert.ToInt64(e).ToString(System.Globalization.CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL",
            _ => "'" + value.ToString()!.Replace("'", "''") + "'"
        };
    }

    private static object? Evaluate(Expression expression)
    {
        if (expression is ConstantExpression c) return c.Value;
        var lambda = Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object)));
        return lambda.Compile().Invoke();
    }

    private static MemberExpression GetMemberExpression(Expression body)
    {
        if (body is UnaryExpression unary) body = unary.Operand;
        return body as MemberExpression
            ?? throw new NotSupportedException("Only simple member expressions are supported.");
    }
}
