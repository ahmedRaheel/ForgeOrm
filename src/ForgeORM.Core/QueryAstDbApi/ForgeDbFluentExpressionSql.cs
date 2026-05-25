using System.Linq.Expressions;
using System.Text;

namespace ForgeORM.Core;

internal static class ForgeDbFluentExpressionSql
{
    public static string TranslateWhere<T>(Expression<Func<T, bool>> expression) => Translate(expression.Body);

    private static string Translate(Expression expression)
    {
        if (expression is BinaryExpression b)
        {
            if (b.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                var logical = b.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({Translate(b.Left)}) {logical} ({Translate(b.Right)})";
            }
            return $"{Member(b.Left)} {Operator(b.NodeType)} {Value(b.Right)}";
        }
        throw new NotSupportedException($"Expression {expression.NodeType} is not supported by ForgeORM fluent CTE/temp SQL.");
    }

    private static string Member(Expression expression)
    {
        if (expression is UnaryExpression u) expression = u.Operand;
        if (expression is MemberExpression m && m.Expression?.NodeType == ExpressionType.Parameter)
            return m.Member.Name;
        throw new NotSupportedException("Left side must be an entity member.");
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
        var value = expression is ConstantExpression c ? c.Value : Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile().Invoke();
        return value switch
        {
            null => "NULL",
            string s => "'" + s.Replace("'", "''") + "'",
            DateTime d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss") + "'",
            DateTimeOffset d => "'" + d.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + "'",
            bool b => b ? "1" : "0",
            Enum e => Convert.ToInt64(e).ToString(System.Globalization.CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL",
            _ => "'" + value.ToString()!.Replace("'", "''") + "'"
        };
    }
}
