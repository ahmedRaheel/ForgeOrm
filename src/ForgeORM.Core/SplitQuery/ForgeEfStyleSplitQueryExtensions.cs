using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// EF-style Include/ThenInclude split-query surface for ForgeORM.
/// These methods intentionally keep the normal db.Set&lt;T&gt;() API and avoid Dapper-style splitOn/multi-mapping.
/// </summary>
public static class ForgeEfStyleSplitQueryExtensions
{
    private static readonly ConditionalWeakTable<object, ForgeEfSplitQueryOptions> Options = new();

    /// <summary>Forces included navigations to be loaded by separate SQL queries.</summary>
    public static IForgeQuery<T> AsSplitQuery<T>(this IForgeQuery<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var options = Options.GetOrCreateValue(query);
        options.Mode = ForgeEfSplitQueryMode.SplitQuery;
        return query;
    }

    /// <summary>
    /// Keeps API parity with EF Core. ForgeORM keeps split query as the safe default for includes;
    /// single-query row-flattening is intentionally not used for collection graphs unless a future provider implements it.
    /// </summary>
    public static IForgeQuery<T> AsSingleQuery<T>(this IForgeQuery<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var options = Options.GetOrCreateValue(query);
        options.Mode = ForgeEfSplitQueryMode.SingleQueryRequested;
        return query;
    }

    /// <summary>Enables reference reuse while graph fixup assigns included entities.</summary>
    public static IForgeQuery<T> UseIdentityResolution<T>(this IForgeQuery<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        Options.GetOrCreateValue(query).UseIdentityResolution = true;
        return query;
    }

    /// <summary>
    /// Compile-friendly ThenInclude hook. Direct includes are fully supported by ForgeORM's graph loader.
    /// Nested includes are recorded for future provider-specific deep graph expansion without changing public syntax.
    /// </summary>
    public static IForgeIncludableQuery<TRoot, TProperty> ThenInclude<TRoot, TPrevious, TProperty>(
        this IForgeIncludableQuery<TRoot, IEnumerable<TPrevious>> query,
        Expression<Func<TPrevious, TProperty>> navigation)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(navigation);
        Options.GetOrCreateValue(query.Query).ThenIncludes.Add(navigation);
        return new ForgeIncludableQuery<TRoot, TProperty>(query.Query);
    }

    public static IForgeIncludableQuery<TRoot, TProperty> ThenInclude<TRoot, TPrevious, TProperty>(
        this IForgeIncludableQuery<TRoot, ICollection<TPrevious>> query,
        Expression<Func<TPrevious, TProperty>> navigation)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(navigation);
        Options.GetOrCreateValue(query.Query).ThenIncludes.Add(navigation);
        return new ForgeIncludableQuery<TRoot, TProperty>(query.Query);
    }

    public static IForgeIncludableQuery<TRoot, TProperty> ThenInclude<TRoot, TPrevious, TProperty>(
        this IForgeIncludableQuery<TRoot, List<TPrevious>> query,
        Expression<Func<TPrevious, TProperty>> navigation)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(navigation);
        Options.GetOrCreateValue(query.Query).ThenIncludes.Add(navigation);
        return new ForgeIncludableQuery<TRoot, TProperty>(query.Query);
    }

    public static IForgeIncludableQuery<TRoot, TProperty> ThenInclude<TRoot, TPrevious, TProperty>(
        this IForgeIncludableQuery<TRoot, TPrevious> query,
        Expression<Func<TPrevious, TProperty>> navigation)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(navigation);
        Options.GetOrCreateValue(query.Query).ThenIncludes.Add(navigation);
        return new ForgeIncludableQuery<TRoot, TProperty>(query.Query);
    }

    internal static ForgeEfSplitQueryOptions GetEfSplitOptions<T>(IForgeQuery<T> query)
        => Options.TryGetValue(query, out var options) ? options : ForgeEfSplitQueryOptions.Default;
}

internal enum ForgeEfSplitQueryMode
{
    SplitQuery,
    SingleQueryRequested
}

internal sealed class ForgeEfSplitQueryOptions
{
    public static readonly ForgeEfSplitQueryOptions Default = new();
    public ForgeEfSplitQueryMode Mode { get; set; } = ForgeEfSplitQueryMode.SplitQuery;
    public bool UseIdentityResolution { get; set; }
    public List<LambdaExpression> ThenIncludes { get; } = [];
}
