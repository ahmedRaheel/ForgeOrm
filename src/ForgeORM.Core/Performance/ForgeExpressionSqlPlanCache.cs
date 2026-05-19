using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

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
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}

public readonly record struct ForgeExpressionSqlKey(Type EntityType, string ProviderName, string ExpressionFingerprint);
