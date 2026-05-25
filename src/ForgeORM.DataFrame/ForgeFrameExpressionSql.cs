using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Core;

namespace ForgeORM.DataFrame;

internal static class ForgeFrameExpressionSql
{
    public static string ToSql(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => $"{ToSql(binary.Left)} {ToOperator(binary.NodeType)} {ToSql(binary.Right)}",
            MemberExpression member when member.Expression is not null && member.Expression.NodeType == ExpressionType.Parameter => member.Member.Name,
            MemberExpression member => FormatValue(Evaluate(member)),
            ConstantExpression constant => FormatValue(constant.Value),
            UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked => ToSql(unary.Operand),
            MethodCallExpression call when call.Method.Name == nameof(string.Contains) && call.Object is not null =>
                $"{ToSql(call.Object)} LIKE '%' + {ToSql(call.Arguments[0])} + '%'",
            MethodCallExpression call when call.Method.Name == nameof(string.StartsWith) && call.Object is not null =>
                $"{ToSql(call.Object)} LIKE {ToSql(call.Arguments[0])} + '%'",
            MethodCallExpression call when call.Method.Name == nameof(string.EndsWith) && call.Object is not null =>
                $"{ToSql(call.Object)} LIKE '%' + {ToSql(call.Arguments[0])}",
            _ => throw new NotSupportedException($"ForgeFrame expression is not supported: {expression}")
        };
    }

    public static string GetMemberName(Expression expression)
    {
        expression = StripConvert(expression);
        if (expression is MemberExpression member)
            return member.Member.Name;

        throw new NotSupportedException($"Only direct member selectors are supported by ForgeFrame aggregate expressions: {expression}");
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression unary && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
            expression = unary.Operand;
        return expression;
    }

    private static string ToOperator(ExpressionType type) => type switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "<>",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        ExpressionType.AndAlso => "AND",
        ExpressionType.OrElse => "OR",
        _ => throw new NotSupportedException($"ForgeFrame operator is not supported: {type}")
    };

    private static object? Evaluate(Expression expression)
    {
        var lambda = Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object)));
        return lambda.Compile().Invoke();
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => "'" + s.Replace("'", "''") + "'",
            Guid g => "'" + g + "'",
            DateTime dt => "'" + dt.ToString("O", CultureInfo.InvariantCulture) + "'",
            DateTimeOffset dto => "'" + dto.ToString("O", CultureInfo.InvariantCulture) + "'",
            bool b => b ? "1" : "0",
            Enum e => Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => "'" + Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("'", "''") + "'"
        };
    }
}
