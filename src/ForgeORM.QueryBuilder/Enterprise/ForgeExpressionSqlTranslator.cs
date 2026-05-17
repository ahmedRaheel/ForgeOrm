using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.QueryBuilder.Enterprise;

/// <summary>
/// Converts simple LINQ expressions into parameterized SQL predicates.
/// </summary>
public sealed class ForgeExpressionSqlTranslator
{
    private readonly List<ForgeSqlParameter> _parameters = [];
    private int _index;

    /// <summary>
    /// Translates a predicate expression to SQL.
    /// </summary>
    public ForgeSqlQuery Translate<T>(Expression<Func<T, bool>> expression)
    {
        _parameters.Clear();
        _index = 0;
        var where = VisitExpression(expression.Body);
        return new ForgeSqlQuery { Sql = where, Parameters = _parameters.ToArray() };
    }

    private string VisitExpression(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => VisitBinary(binary),
            MemberExpression member => VisitMember(member),
            ConstantExpression constant => AddParameter(constant.Value),
            UnaryExpression unary when unary.NodeType == ExpressionType.Convert => VisitExpression(unary.Operand),
            MethodCallExpression method => VisitMethod(method),
            _ => throw new NotSupportedException($"Expression '{expression.NodeType}' is not supported by ForgeORM translator.")
        };
    }

    private string VisitBinary(BinaryExpression expression)
    {
        var left = VisitExpression(expression.Left);
        var right = VisitExpression(expression.Right);
        var op = expression.NodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Binary operator '{expression.NodeType}' is not supported.")
        };

        return $"({left} {op} {right})";
    }

    private string VisitMember(MemberExpression expression)
    {
        if (expression.Expression?.NodeType == ExpressionType.Parameter)
        {
            return expression.Member.Name;
        }

        return AddParameter(Evaluate(expression));
    }

    private string VisitMethod(MethodCallExpression expression)
    {
        if (expression.Object is MemberExpression member && expression.Method.Name is "Contains" or "StartsWith" or "EndsWith")
        {
            var column = VisitMember(member);
            var value = Evaluate(expression.Arguments[0])?.ToString() ?? string.Empty;
            var pattern = expression.Method.Name switch
            {
                "StartsWith" => value + "%",
                "EndsWith" => "%" + value,
                _ => "%" + value + "%"
            };

            return $"({column} LIKE {AddParameter(pattern)})";
        }

        throw new NotSupportedException($"Method '{expression.Method.Name}' is not supported.");
    }

    private string AddParameter(object? value)
    {
        var name = $"@p{_index++}";
        _parameters.Add(new ForgeSqlParameter(name, value));
        return name;
    }

    private static object? Evaluate(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        var converted = Expression.Convert(expression, typeof(object));
        return Expression.Lambda<Func<object?>>(converted).Compile().Invoke();
    }
}
