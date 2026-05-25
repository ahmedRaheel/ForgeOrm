using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>Small in-memory compiled/rendered query cache foundation.</summary>
public static class ForgeQueryResultCache
{
    private static readonly Dictionary<string, (DateTimeOffset Expires, object Rows)> Cache = new(StringComparer.Ordinal);

    public static async ValueTask<IReadOnlyList<T>> GetOrExecuteAsync<T>(string sql, IReadOnlyDictionary<string, object?> parameters, TimeSpan? duration, Func<ValueTask<IReadOnlyList<T>>> factory, CancellationToken cancellationToken)
    {
        if (duration is null || duration.Value <= TimeSpan.Zero)
        {
            return await factory();
        }

        var key = sql + "|" + string.Join(";", parameters.OrderBy(x => x.Key).Select(x => x.Key + "=" + x.Value));
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var hit) && hit.Expires > DateTimeOffset.UtcNow && hit.Rows is IReadOnlyList<T> typed)
            {
                return typed;
            }
        }

        var rows = await factory();
        lock (Cache) Cache[key] = (DateTimeOffset.UtcNow.Add(duration.Value), rows);
        return rows;
    }
}
