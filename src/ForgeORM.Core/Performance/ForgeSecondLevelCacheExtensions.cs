using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;

namespace ForgeORM.Core;

public static class ForgeSecondLevelCacheExtensions
{
    /// <summary>Enables opt-in second-level result caching for this query instance.</summary>
    public static IForgeQuery<T> CacheFor<T>(this IForgeQuery<T> query, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        var state = ForgeSecondLevelQueryCache.State(query);
        state.CacheDuration = duration;
        state.BypassCache = false;
        return query;
    }

    /// <summary>Disables second-level cache for this query instance even if default rules are added later.</summary>
    public static IForgeQuery<T> NoCache<T>(this IForgeQuery<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var state = ForgeSecondLevelQueryCache.State(query);
        state.BypassCache = true;
        state.CacheDuration = null;
        return query;
    }

    /// <summary>
    /// Warms metadata, generated/runtime readers, generated SQL, and the provider-direct GetById plan for an entity type.
    /// Safe to call on app startup.
    /// </summary>
    public static ValueTask WarmupAsync<T>(this ForgeDb db, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        cancellationToken.ThrowIfCancellationRequested();
        _ = ForgeEntityMetadataCache.Get(typeof(T));
        _ = ForgeCompiledIncludePlanCache.GetOrCreate(typeof(T), Array.Empty<PropertyInfo>(), false, false);
        _ = ForgeProjectionReaderCache.GetOrCreate(typeof(T), typeof(T), null);
        return ValueTask.CompletedTask;
    }
}
