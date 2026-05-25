namespace ForgeORM.Core.SplitQuery;

internal sealed class ForgeSplitIncludeManyToMany<TParent, TJoin, TChild, TParentKey, TChildKey> : IForgeSplitInclude<TParent>
    where TParent : class
    where TJoin : class
    where TChild : class
    where TParentKey : notnull
    where TChildKey : notnull
{
    private readonly Func<IEnumerable<TParentKey>, string> _joinSqlFactory;
    private readonly Func<IEnumerable<TChildKey>, string> _childSqlFactory;
    private readonly Func<TParent, TParentKey> _parentKey;
    private readonly Func<TJoin, TParentKey> _joinParentKey;
    private readonly Func<TJoin, TChildKey> _joinChildKey;
    private readonly Func<TChild, TChildKey> _childKey;
    private readonly Action<TParent, IReadOnlyList<TChild>> _assign;

    public ForgeSplitIncludeManyToMany(
        Func<IEnumerable<TParentKey>, string> joinSqlFactory,
        Func<IEnumerable<TChildKey>, string> childSqlFactory,
        Func<TParent, TParentKey> parentKey,
        Func<TJoin, TParentKey> joinParentKey,
        Func<TJoin, TChildKey> joinChildKey,
        Func<TChild, TChildKey> childKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
    {
        _joinSqlFactory = joinSqlFactory;
        _childSqlFactory = childSqlFactory;
        _parentKey = parentKey;
        _joinParentKey = joinParentKey;
        _joinChildKey = joinChildKey;
        _childKey = childKey;
        _assign = assign;
    }

    public async ValueTask ApplyAsync(
        ForgeDbContext db,
        IReadOnlyList<TParent> parents,
        CancellationToken cancellationToken)
    {
        if (parents.Count == 0)
        {
            return;
        }

        var parentIds = parents.Select(_parentKey).Distinct().ToArray();
        var joins = (await db.QueryAsync<TJoin>(
            _joinSqlFactory(parentIds),
            new { Ids = parentIds },
            cancellationToken: cancellationToken)).ToList();

        var childIds = joins.Select(_joinChildKey).Distinct().ToArray();
        if (childIds.Length == 0)
        {
            foreach (var parent in parents)
                _assign(parent, Array.Empty<TChild>());
            return;
        }

        var children = (await db.QueryAsync<TChild>(
            _childSqlFactory(childIds),
            new { Ids = childIds },
            cancellationToken: cancellationToken)).ToList();

        var childById = children.ToDictionary(_childKey);
        var joinsByParent = joins.ToLookup(_joinParentKey);

        foreach (var parent in parents)
        {
            var related = joinsByParent[_parentKey(parent)]
                .Select(_joinChildKey)
                .Where(childById.ContainsKey)
                .Select(id => childById[id])
                .ToList();

            _assign(parent, related);
        }
    }
}
