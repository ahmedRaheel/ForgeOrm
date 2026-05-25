using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Common;
using ForgeORM.Core.Performance;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal static class ForgeExpressionTranslator
{
    private static readonly ConcurrentDictionary<string, string> PredicateSqlCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, string> MemberNameCache = new(StringComparer.Ordinal);
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the T operation.</returns>
    public static string Translate<T>(Expression<Func<T, bool>> expression)
    {
        var cacheKey = typeof(T).FullName + ":" + expression;
        return PredicateSqlCache.GetOrAdd(cacheKey, _ => TranslateCore(expression.Body));
    }

    private static string TranslateCore(Expression body)
    {
        if (body is not BinaryExpression b) throw new NotSupportedException("Only simple binary expressions are supported in MVP.");
        return $"{Member(b.Left)} {Operator(b.NodeType)} {Value(b.Right)}";
    }
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="expression">The expression value.</param>
    /// <returns>The result of the T operation.</returns>
    public static string MemberName<T>(Expression<Func<T, object>> expression)
    {
        var cacheKey = typeof(T).FullName + ":" + expression;
        return MemberNameCache.GetOrAdd(cacheKey, _ => MemberNameCore(expression));
    }

    public static string MemberName(LambdaExpression expression)
    {
        var cacheKey = expression.ReturnType.FullName + ":" + expression;
        return MemberNameCache.GetOrAdd(cacheKey, _ => MemberNameCore(expression));
    }

    private static string MemberNameCore(LambdaExpression expression)
    {
        Expression body = expression.Body is UnaryExpression u ? u.Operand : expression.Body;
        return body is MemberExpression m ? m.Member.Name : throw new NotSupportedException("Only member expression is supported.");
    }
    private static string Member(Expression e) => e is MemberExpression m ? m.Member.Name : throw new NotSupportedException("Left side must be member.");
    private static string Operator(ExpressionType t) => t switch { ExpressionType.Equal => "=", ExpressionType.NotEqual => "<>", ExpressionType.GreaterThan => ">", ExpressionType.GreaterThanOrEqual => ">=", ExpressionType.LessThan => "<", ExpressionType.LessThanOrEqual => "<=", _ => throw new NotSupportedException("Operator not supported.") };
    private static string Value(Expression e)
    {
        var v = ForgeExpressionDelegateCache.Evaluate(e);
        return v switch { null => "NULL", string s => "'" + s.Replace("'", "''") + "'", DateTime d => "'" + d.ToString("yyyy-MM-dd HH:mm:ss") + "'", bool b => b ? "1" : "0", _ => v?.ToString() ?? "NULL" };
    }
}
