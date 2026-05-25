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

internal interface IForgeSplitInclude<TParent>
    where TParent : class
{
    ValueTask ApplyAsync(
        ForgeDbContext db,
        IReadOnlyList<TParent> parents,
        CancellationToken cancellationToken);
}

internal sealed class ForgeSplitIncludeOne<TParent, TChild, TKey> : IForgeSplitInclude<TParent>
    where TParent : class
    where TChild : class
    where TKey : notnull
{
    private readonly Func<IEnumerable<TKey>, string> _childSqlFactory;
    private readonly Func<TParent, TKey> _parentKey;
    private readonly Func<TChild, TKey> _childForeignKey;
    private readonly Action<TParent, TChild?> _assign;

    public ForgeSplitIncludeOne(
        Func<IEnumerable<TKey>, string> childSqlFactory,
        Func<TParent, TKey> parentKey,
        Func<TChild, TKey> childForeignKey,
        Action<TParent, TChild?> assign)
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
        var children = await db.QueryAsync<TChild>(
            _childSqlFactory(ids),
            new { Ids = ids },
            cancellationToken: cancellationToken);

        var map = children
            .GroupBy(_childForeignKey)
            .ToDictionary(x => x.Key, x => x.FirstOrDefault());

        foreach (var parent in parents)
        {
            map.TryGetValue(_parentKey(parent), out var child);
            _assign(parent, child);
        }
    }
}

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


internal static class ForgeSplitSqlInList
{
    public static string ExpandIds<TKey>(string sql, IReadOnlyCollection<TKey> ids)
        where TKey : notnull
    {
        if (ids.Count == 0)
            return sql.Replace("IN @Ids", "IN (NULL)", StringComparison.OrdinalIgnoreCase)
                      .Replace("IN (@Ids)", "IN (NULL)", StringComparison.OrdinalIgnoreCase);

        var list = string.Join(", ", ids.Select(FormatLiteral));
        return sql.Replace("IN @Ids", "IN (" + list + ")", StringComparison.OrdinalIgnoreCase)
                  .Replace("IN (@Ids)", "IN (" + list + ")", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatLiteral<TKey>(TKey value)
    {
        if (value is null)
            return "NULL";

        return value switch
        {
            string text => "'" + text.Replace("'", "''") + "'",
            Guid guid => "'" + guid.ToString("D") + "'",
            DateTime dateTime => "'" + dateTime.ToString("O") + "'",
            DateTimeOffset dateTimeOffset => "'" + dateTimeOffset.ToString("O") + "'",
            Enum enumValue => Convert.ToInt64(enumValue).ToString(System.Globalization.CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => "'" + value.ToString()!.Replace("'", "''") + "'"
        };
    }
}
