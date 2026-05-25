using System.Linq.Expressions;
using System.Reflection;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal static class ForgeCoreExpressionSql
{
    public static ForgeCoreSqlCondition Translate<T>(Expression<Func<T, bool>> predicate, ForgeEntityMetadata metadata, int startIndex = 0)
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var sql = TranslateNode(predicate.Body, metadata, parameters, ref startIndex);
        return new ForgeCoreSqlCondition(sql, parameters);
    }

    private static string TranslateNode(Expression expression, ForgeEntityMetadata metadata, Dictionary<string, object?> parameters, ref int index)
    {
        expression = StripConvert(expression);

        if (expression is BinaryExpression binary)
        {
            if (binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                var left = TranslateNode(binary.Left, metadata, parameters, ref index);
                var right = TranslateNode(binary.Right, metadata, parameters, ref index);
                var op1 = binary.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({left}) {op1} ({right})";
            }

            var leftMember = ResolveMemberSql(binary.Left, metadata);
            var op = ResolveOperator(binary.NodeType);
            var rightValue = Evaluate(binary.Right);

            if (rightValue is null && binary.NodeType == ExpressionType.Equal)
                return $"{leftMember} IS NULL";

            if (rightValue is null && binary.NodeType == ExpressionType.NotEqual)
                return $"{leftMember} IS NOT NULL";

            var parameterName = "p" + index++;
            parameters[parameterName] = rightValue;
            return $"{leftMember} {op} @{parameterName}";
        }

        if (expression is UnaryExpression { NodeType: ExpressionType.Not } unary)
        {
            var column = ResolveMemberSql(unary.Operand, metadata);
            return $"{column} = 0";
        }

        if (expression is MemberExpression member && member.Type == typeof(bool))
        {
            var column = ResolveMemberSql(member, metadata);
            return $"{column} = 1";
        }

        throw new NotSupportedException("Only simple boolean, binary, AND, and OR expressions are supported.");
    }

    private static string ResolveMemberSql(Expression expression, ForgeEntityMetadata metadata)
    {
        expression = StripConvert(expression);
        if (expression is MemberExpression member)
        {
            var propertyName = member.Member.Name;
            return metadata.Properties.FirstOrDefault(x => x.PropertyName.Equals(propertyName, StringComparison.OrdinalIgnoreCase))?.ColumnName ?? propertyName;
        }

        throw new NotSupportedException("Left side must be a member expression.");
    }

    private static string ResolveOperator(ExpressionType type) => type switch
    {
        ExpressionType.Equal => "=",
        ExpressionType.NotEqual => "<>",
        ExpressionType.GreaterThan => ">",
        ExpressionType.GreaterThanOrEqual => ">=",
        ExpressionType.LessThan => "<",
        ExpressionType.LessThanOrEqual => "<=",
        _ => throw new NotSupportedException($"Operator {type} is not supported.")
    };

    private static object? Evaluate(Expression expression)
    {
        expression = StripConvert(expression);
        if (expression is ConstantExpression constant)
            return constant.Value;

        return ForgeExpressionDelegateCache.Evaluate(expression);
    }

    private static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression unary && unary.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
            expression = unary.Operand;

        return expression;
    }
}
