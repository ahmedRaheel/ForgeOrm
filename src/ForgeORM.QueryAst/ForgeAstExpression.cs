using System.Linq.Expressions;

namespace ForgeORM.QueryAst;

internal static class ForgeAstExpression
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the T operation.</returns>
    public static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        Expression body = expression.Body;

        if (body is UnaryExpression unary)
            body = unary.Operand;

        return body is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Only simple member expressions are supported.");
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <param name="startIndex">The startIndex value.</param>
    /// <returns>The result of the T operation.</returns>
    public static ForgeExpressionResult Translate<T>(Expression<Func<T, bool>> expression, int startIndex = 0)
    {
        var parameters = new Dictionary<string, object?>();
        var sql = TranslateNode(expression.Body, parameters, ref startIndex);
        return new ForgeExpressionResult { Sql = sql, Parameters = parameters };
    }

    private static string TranslateNode(Expression expression, Dictionary<string, object?> parameters, ref int index)
    {
        if (expression is BinaryExpression binary)
        {
            if (binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                var leftLogical = TranslateNode(binary.Left, parameters, ref index);
                var rightLogical = TranslateNode(binary.Right, parameters, ref index);
                var logical = binary.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({leftLogical}) {logical} ({rightLogical})";
            }

            var left = TranslateMember(binary.Left);
            var op = Operator(binary.NodeType);
            var right = AddParameter(Evaluate(binary.Right), parameters, ref index);
            return $"{left} {op} {right}";
        }

        throw new NotSupportedException("Only simple binary expressions are supported in the AST MVP.");
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the T operation.</returns>
    public static string Translate<T>(Expression<Func<T, bool>> expression)
    {
        if (expression.Body is not BinaryExpression binary)
            throw new NotSupportedException("Only simple binary expressions are supported in the AST MVP.");

        return $"{Member(binary.Left)} {Operator(binary.NodeType)} {Value(binary.Right)}";
    }
    private static string TranslateMember(Expression expression)
    {
        return expression is MemberExpression member
            ? member.Member.Name
            : throw new NotSupportedException("Left side must be a member expression.");
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
    private static string AddParameter(object? value, Dictionary<string, object?> parameters, ref int index)
    {
        var name = "p" + index++;
        parameters[name] = value;
        return "@" + name;
    }

    private static object? Evaluate(Expression expression)
    {
        return Expression.Lambda(expression).Compile().DynamicInvoke();
    }
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
