using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using ForgeORM.Abstractions;
using ForgeORM.Core.Graph;

namespace ForgeORM.Core;

/// <summary>
/// Lightweight in-process second-level result cache and query performance state.
/// This is intentionally opt-in per query through CacheFor(...) so normal queries keep the fastest direct path.
/// </summary>
internal static class ForgeSecondLevelQueryCache
{
    private sealed class CacheEntry
    {
        public required object Value { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
    }

    internal sealed class QueryState
    {
        public TimeSpan? CacheDuration { get; set; }
        public bool BypassCache { get; set; }
    }

    private static readonly ConditionalWeakTable<object, QueryState> QueryStates = new();
    private static readonly ConcurrentDictionary<string, CacheEntry> Entries = new(StringComparer.Ordinal);

    public static QueryState State(object query) => QueryStates.GetOrCreateValue(query);

    public static bool TryGetList<T>(object query, string cacheKey, out IReadOnlyList<T> rows)
    {
        rows = Array.Empty<T>();
        var state = State(query);
        if (state.BypassCache || state.CacheDuration is null)
            return false;

        if (!Entries.TryGetValue(cacheKey, out var entry))
            return false;

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            Entries.TryRemove(cacheKey, out _);
            return false;
        }

        if (entry.Value is IReadOnlyList<T> typed)
        {
            rows = typed;
            return true;
        }

        return false;
    }

    public static void SetList<T>(object query, string cacheKey, IReadOnlyList<T> rows)
    {
        var state = State(query);
        if (state.BypassCache || state.CacheDuration is not { } duration || duration <= TimeSpan.Zero)
            return;

        Entries[cacheKey] = new CacheEntry
        {
            Value = rows,
            ExpiresAt = DateTimeOffset.UtcNow.Add(duration)
        };
    }

    public static string BuildKey(Type resultType, string sql, object? parameters, IReadOnlyList<PropertyInfo> includes, ForgeQueryExecutionOptions options)
    {
        var sb = new StringBuilder(256);
        sb.Append(resultType.FullName).Append('|').Append(sql).Append('|');
        AppendParameters(sb, parameters);
        if (includes.Count > 0)
        {
            sb.Append("|inc:");
            for (var i = 0; i < includes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(includes[i].Name);
            }
        }
        if (!string.IsNullOrWhiteSpace(options.QueryTag)) sb.Append("|tag:").Append(options.QueryTag);
        if (options.UseReadReplica) sb.Append("|replica");
        if (options.LockBehavior != ForgeLockBehavior.None) sb.Append("|lock:").Append(options.LockBehavior);
        return sb.ToString();
    }

    private static void AppendParameters(StringBuilder sb, object? parameters)
    {
        if (parameters is null) return;
        if (parameters is IReadOnlyDictionary<string, object?> ro)
        {
            foreach (var item in ro.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                sb.Append(item.Key).Append('=').Append(item.Value).Append(';');
            return;
        }
        if (parameters is System.Collections.IDictionary dict)
        {
            var keys = dict.Keys.Cast<object>().Select(x => x.ToString() ?? string.Empty).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys)
                sb.Append(key).Append('=').Append(dict[key]).Append(';');
            return;
        }
        if (ForgeMaterializer.IsScalar(parameters.GetType()))
        {
            sb.Append("value=").Append(parameters);
            return;
        }
        foreach (var prop in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            sb.Append(prop.Name).Append('=').Append(ForgeRuntimeAccessorCache.Get(prop, parameters)).Append(';');
    }
}

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
    public static Task WarmupAsync<T>(this ForgeDb db, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        cancellationToken.ThrowIfCancellationRequested();
        _ = ForgeEntityMetadataCache.Get(typeof(T));
        _ = ForgeCompiledIncludePlanCache.GetOrCreate(typeof(T), Array.Empty<PropertyInfo>(), false, false);
        _ = ForgeProjectionReaderCache.GetOrCreate(typeof(T), typeof(T), null);
        return Task.CompletedTask;
    }
}
