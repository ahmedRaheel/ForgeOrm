using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

namespace ForgeORM.Core;

/// <summary>
/// Caches projection delegates by expression fingerprint. The expression is compiled once per shape and then reused.
/// </summary>
public static class ForgeProjectionPlanCache
{
    private static readonly ConcurrentDictionary<string, Delegate> Cache = new(StringComparer.Ordinal);

    public static Func<TSource, TProjection> GetOrAdd<TSource, TProjection>(Expression<Func<TSource, TProjection>> projection)
    {
        var key = typeof(TSource).FullName + "->" + typeof(TProjection).FullName + ":" + Fingerprint(projection.ToString());
        return (Func<TSource, TProjection>)Cache.GetOrAdd(key, _ => projection.Compile());
    }

    private static string Fingerprint(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
