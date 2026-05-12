using ForgeORM.Abstractions;

namespace ForgeORM.Relationships;

public static class ForgeRelationshipSplitQueryExtensions
{
    public static ForgeRelationshipSplitQuery<TParent> SplitGraph<TParent>(this IForgeDb db) => new(db);
}

public sealed class ForgeRelationshipSplitQuery<TParent>
{
    private readonly IForgeDb _db;
    private readonly List<Func<IReadOnlyList<TParent>, CancellationToken, Task>> _loaders = [];

    public ForgeRelationshipSplitQuery(IForgeDb db) => _db = db;

    public ForgeRelationshipSplitQuery<TParent> IncludeOne<TChild, TKey>(
        Func<IReadOnlyCollection<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, TChild?> assign)
        where TKey : notnull
    {
        _loaders.Add(async (parents, ct) =>
        {
            var keys = parents.Select(parentKey).Distinct().ToList();
            if (keys.Count == 0) return;

            var children = await _db.QueryAsync<TChild>(childSqlFactory(keys), new { Ids = keys }, cancellationToken: ct);
            var lookup = children.GroupBy(childForeignKey).ToDictionary(x => x.Key, x => x.FirstOrDefault());

            foreach (var parent in parents)
            {
                var key = parentKey(parent);
                assign(parent, lookup.TryGetValue(key, out var child) ? child : default);
            }
        });
        return this;
    }

    public ForgeRelationshipSplitQuery<TParent> IncludeMany<TChild, TKey>(
        Func<IReadOnlyCollection<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
        where TKey : notnull
    {
        _loaders.Add(async (parents, ct) =>
        {
            var keys = parents.Select(parentKey).Distinct().ToList();
            if (keys.Count == 0) return;

            var children = await _db.QueryAsync<TChild>(childSqlFactory(keys), new { Ids = keys }, cancellationToken: ct);
            var lookup = children.GroupBy(childForeignKey).ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList());

            foreach (var parent in parents)
            {
                var key = parentKey(parent);
                assign(parent, lookup.TryGetValue(key, out var rows) ? rows : []);
            }
        });
        return this;
    }

    public ForgeRelationshipSplitQuery<TParent> IncludeManyToMany<TJoin, TChild, TParentKey, TChildKey>(
        Func<IReadOnlyCollection<TParentKey>, string> joinSqlFactory,
        Func<IReadOnlyCollection<TChildKey>, string> childSqlFactory,
        Func<TParent, TParentKey> parentKey,
        Func<TJoin, TParentKey> joinParentKey,
        Func<TJoin, TChildKey> joinChildKey,
        Func<TChild, TChildKey> childKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
        where TParentKey : notnull
        where TChildKey : notnull
    {
        _loaders.Add(async (parents, ct) =>
        {
            var parentKeys = parents.Select(parentKey).Distinct().ToList();
            if (parentKeys.Count == 0) return;

            var joins = await _db.QueryAsync<TJoin>(joinSqlFactory(parentKeys), new { Ids = parentKeys }, cancellationToken: ct);
            var childKeys = joins.Select(joinChildKey).Distinct().ToList();

            if (childKeys.Count == 0)
            {
                foreach (var parent in parents) assign(parent, []);
                return;
            }

            var children = await _db.QueryAsync<TChild>(childSqlFactory(childKeys), new { Ids = childKeys }, cancellationToken: ct);
            var childLookup = children.ToDictionary(childKey);
            var joinLookup = joins.GroupBy(joinParentKey).ToDictionary(x => x.Key, x => x.Select(joinChildKey).ToList());

            foreach (var parent in parents)
            {
                var key = parentKey(parent);

                if (!joinLookup.TryGetValue(key, out var relatedKeys))
                {
                    assign(parent, []);
                    continue;
                }

                assign(parent, relatedKeys.Where(childLookup.ContainsKey).Select(x => childLookup[x]).ToList());
            }
        });
        return this;
    }

    public IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null) => ToListAsync(parentSql, parameters).GetAwaiter().GetResult();

    public async Task<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var parents = (await _db.QueryAsync<TParent>(parentSql, parameters, cancellationToken: cancellationToken)).ToList();
        foreach (var loader in _loaders) await loader(parents, cancellationToken);
        return parents;
    }
}
