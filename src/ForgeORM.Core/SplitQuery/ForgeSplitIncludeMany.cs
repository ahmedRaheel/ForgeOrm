namespace ForgeORM.Core.SplitQuery;

internal sealed class ForgeSplitIncludeMany<TParent, TChild, TKey> : IForgeSplitInclude<TParent>
    where TParent : class
    where TChild : class
    where TKey : notnull
{
    private readonly Func<IEnumerable<TKey>, string> _childSqlFactory;
    private readonly Func<TParent, TKey> _parentKey;
    private readonly Func<TChild, TKey> _childForeignKey;
    private readonly Action<TParent, IReadOnlyList<TChild>> _assign;

    public ForgeSplitIncludeMany(
        Func<IEnumerable<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
    {
        _childSqlFactory = childSqlFactory;
        _parentKey = parentKey;
        _childForeignKey = childForeignKey;
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

        var ids = parents.Select(_parentKey).Distinct().ToArray();
        var children = (await db.QueryAsync<TChild>(
            _childSqlFactory(ids),
            new { Ids = ids },
            cancellationToken: cancellationToken)).ToList();

        var lookup = children.ToLookup(_childForeignKey);

        foreach (var parent in parents)
        {
            _assign(parent, lookup[_parentKey(parent)].ToList());
        }
    }
}
