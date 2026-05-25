namespace ForgeORM.Core.SplitQuery;

/// <summary>
/// Minimal compile-safe split graph query builder.
/// It loads parent rows first and then executes child include queries using the collected parent ids.
/// </summary>
public sealed class ForgeSplitGraphBuilder<TParent>
    where TParent : class
{
    private readonly ForgeDbContext _db;
    private readonly List<IForgeSplitInclude<TParent>> _includes = [];

    public ForgeSplitGraphBuilder(ForgeDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Includes one child row per parent.
    /// </summary>
    public ForgeSplitGraphBuilder<TParent> IncludeOne<TChild, TKey>(
        Func<IEnumerable<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, TChild?> assign)
        where TChild : class
        where TKey : notnull
    {
        _includes.Add(new ForgeSplitIncludeOne<TParent, TChild, TKey>(
            childSqlFactory,
            parentKey,
            childForeignKey,
            assign));

        return this;
    }

    /// <summary>
    /// Includes many child rows per parent.
    /// </summary>
    public ForgeSplitGraphBuilder<TParent> IncludeMany<TChild, TKey>(
        Func<IEnumerable<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
        where TChild : class
        where TKey : notnull
    {
        _includes.Add(new ForgeSplitIncludeMany<TParent, TChild, TKey>(
            childSqlFactory,
            parentKey,
            childForeignKey,
            assign));

        return this;
    }

    /// <summary>
    /// Includes a many-to-many relationship through a join entity.
    /// </summary>
    public ForgeSplitGraphBuilder<TParent> IncludeManyToMany<TJoin, TChild, TParentKey, TChildKey>(
        Func<IEnumerable<TParentKey>, string> joinSqlFactory,
        Func<IEnumerable<TChildKey>, string> childSqlFactory,
        Func<TParent, TParentKey> parentKey,
        Func<TJoin, TParentKey> joinParentKey,
        Func<TJoin, TChildKey> joinChildKey,
        Func<TChild, TChildKey> childKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
        where TJoin : class
        where TChild : class
        where TParentKey : notnull
        where TChildKey : notnull
    {
        _includes.Add(new ForgeSplitIncludeManyToMany<TParent, TJoin, TChild, TParentKey, TChildKey>(
            joinSqlFactory,
            childSqlFactory,
            parentKey,
            joinParentKey,
            joinChildKey,
            childKey,
            assign));

        return this;
    }

    /// <summary>
    /// Executes the parent query and all configured split includes.
    /// </summary>
    public async ValueTask<IReadOnlyList<TParent>> ToListAsync(
        string parentSql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var parents = (await _db.QueryAsync<TParent>(
            parentSql,
            parameters,
            cancellationToken: cancellationToken)).ToList();

        foreach (var include in _includes)
        {
            await include.ApplyAsync(_db, parents, cancellationToken);
        }

        return parents;
    }
}
