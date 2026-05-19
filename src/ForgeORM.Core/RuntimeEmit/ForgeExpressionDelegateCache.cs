using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ForgeORM.Core;

internal static class ForgeExpressionDelegateCache
{
    private static readonly ConcurrentDictionary<string, Delegate> Cache = new(StringComparer.Ordinal);

    public static Func<T, TResult> Get<T, TResult>(Expression<Func<T, TResult>> expression)
    {
        var key = typeof(T).FullName + "|" + typeof(TResult).FullName + "|" + expression;
        return (Func<T, TResult>)Cache.GetOrAdd(key, _ => expression.Compile());
    }

    public static object? Evaluate(Expression expression)
    {
        var lambda = Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object)));
        var key = "eval|" + expression.Type.FullName + "|" + expression;
        var compiled = (Func<object?>)Cache.GetOrAdd(key, _ => lambda.Compile());
        return compiled();
    }
}
