using System.Collections.Concurrent;

namespace ForgeORM.Core.SplitQuery;

/// <summary>
/// Adds split-query parent/child loading helpers to ForgeDbContext.
/// </summary>
public static class ForgeSplitGraphExtensions
{
    /// <summary>
    /// Starts a split graph query for loading parent rows first and child rows separately.
    /// </summary>
    public static ForgeSplitGraphBuilder<TParent> SplitGraph<TParent>(this ForgeDbContext db)
        where TParent : class
    {
        ArgumentNullException.ThrowIfNull(db);
        return new ForgeSplitGraphBuilder<TParent>(db);
    }

    /// <summary>
    /// Preferred alias for SplitGraph. Use this name for query/read scenarios.
    /// </summary>
    public static ForgeSplitGraphBuilder<TParent> SplitQuery<TParent>(this ForgeDbContext db)
        where TParent : class
    {
        ArgumentNullException.ThrowIfNull(db);
        return new ForgeSplitGraphBuilder<TParent>(db);
    }
}
