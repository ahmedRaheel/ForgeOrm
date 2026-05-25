using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ForgeORM.Core;

/// <summary>
/// Expression translation cache keyed by expression fingerprint, provider and entity type.
/// </summary>
public static class ForgeExpressionSqlPlanCache
{
    private static readonly ConcurrentDictionary<ForgeExpressionSqlKey, string> Cache = new();

    public static string GetOrAdd<T>(string providerName, Expression<Func<T, bool>> expression, Func<string> translator)
    {
        var key = new ForgeExpressionSqlKey(typeof(T), providerName, Fingerprint(expression.ToString()));
        return Cache.GetOrAdd(key, _ => translator());
    }

    private static string Fingerprint(string text)
        => ForgeFastHash.FingerprintSql(text);
}

public readonly record struct ForgeExpressionSqlKey(Type EntityType, string ProviderName, string ExpressionFingerprint);
