using System.Collections;
using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Compile-friendly EF-style ThenInclude surface. ForgeORM records nested include paths and the
/// EF-style split graph loader materializes them using separate child queries, avoiding Dapper-style
/// multi-mapping and parent row duplication.
/// </summary>
public static class ForgeThenIncludeExtensions
{
    public static IForgeQuery<TRoot> ThenInclude<TRoot, TPrevious, TProperty>(
        this IForgeQuery<TRoot> query,
        Expression<Func<TPrevious, TProperty>> navigation)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(navigation);
        return ForgeEfStyleSplitQueryExtensions.ThenInclude(query, navigation);
    }

    public static IForgeQuery<TRoot> ThenInclude<TRoot, TPrevious, TProperty>(
        this IForgeQuery<TRoot> query,
        Expression<Func<TPrevious, IEnumerable<TProperty>>> navigation)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(navigation);
        return ForgeEfStyleSplitQueryExtensions.ThenInclude(query, navigation);
    }
}
