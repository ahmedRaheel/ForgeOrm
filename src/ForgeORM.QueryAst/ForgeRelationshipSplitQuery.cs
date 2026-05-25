using ForgeORM.Abstractions;
using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.QueryAst;

public sealed class ForgeRelationshipSplitQuery<TParent>
{
    private readonly IForgeDb _db;
    private readonly List<Func<IReadOnlyList<TParent>, CancellationToken, ValueTask>> _loaders = [];
    private string? _parentSql;
    private object? _parentParameters;

    /// <summary>
    /// Executes the ForgeRelationshipSplitQuery operation.
    /// </summary>
    /// <param name="db">The db value.</param>
    /// <returns>The result of the ForgeRelationshipSplitQuery operation.</returns>
    public ForgeRelationshipSplitQuery(IForgeDb db) => _db = db;

    /// <summary>
    /// Uses the default table mapped to the parent type as the graph root.
    /// </summary>
    /// <returns>The same split graph query for fluent chaining.</returns>
    public ForgeRelationshipSplitQuery<TParent> From()
    {
        _parentSql = $"SELECT * FROM {ResolveTableName(typeof(TParent))}";
        return this;
    }

    /// <summary>
    /// Uses a custom SQL query as the graph root.
    /// </summary>
    /// <param name="sql">The SQL statement used to load parent rows.</param>
    /// <param name="parameters">The optional SQL parameters used by the parent query.</param>
    /// <returns>The same split graph query for fluent chaining.</returns>
    public ForgeRelationshipSplitQuery<TParent> FromSql(string sql, object? parameters = null)
    {
        _parentSql = sql;
        _parentParameters = parameters;
        return this;
    }

    /// <summary>
    /// Uses the mapped parent table and adds an expression-based parent filter.
    /// </summary>
    /// <param name="predicate">The parent filter expression.</param>
    /// <returns>The same split graph query for fluent chaining.</returns>
    public ForgeRelationshipSplitQuery<TParent> Where(Expression<Func<TParent, bool>> predicate)
    {
        var result = ForgeAstExpression.Translate(predicate, 0);
        _parentSql = $"SELECT * FROM {ResolveTableName(typeof(TParent))} WHERE {result.Sql}";
        _parentParameters = result.Parameters;
        return this;
    }

    /// <summary>
    /// Uses the mapped parent table and adds a SQL-based parent filter.
    /// </summary>
    /// <param name="condition">The SQL condition without the WHERE keyword.</param>
    /// <param name="parameters">The optional SQL parameters used by the condition.</param>
    /// <returns>The same split graph query for fluent chaining.</returns>
    public ForgeRelationshipSplitQuery<TParent> WhereSql(string condition, object? parameters = null)
    {
        _parentSql = $"SELECT * FROM {ResolveTableName(typeof(TParent))} WHERE {condition}";
        _parentParameters = parameters;
        return this;
    }

    /// <summary>
    /// Includes one child row per parent using mapped table names and key expressions.
    /// </summary>
    /// <typeparam name="TChild">The child entity or DTO type.</typeparam>
    /// <param name="parentKey">The parent key expression.</param>
    /// <param name="childForeignKey">The child foreign-key expression.</param>
    /// <param name="assign">The assignment action used to attach the child to the parent.</param>
    /// <returns>The same split graph query for fluent chaining.</returns>
    public ForgeRelationshipSplitQuery<TParent> IncludeOne<TChild>(
        Expression<Func<TParent, object>> parentKey,
        Expression<Func<TChild, object>> childForeignKey,
        Action<TParent, TChild?> assign)
    {
        var parentKeyName = ForgeAstExpression.MemberName(parentKey);
        var childForeignKeyName = ForgeAstExpression.MemberName(childForeignKey);
        return IncludeOne<TChild, object>(
            _ => $"SELECT * FROM {ResolveTableName(typeof(TChild))} WHERE {childForeignKeyName} IN @ParentIds",
            parent => GetValue<object>(parent!, parentKeyName),
            child => GetValue<object>(child!, childForeignKeyName),
            assign);
    }

    /// <summary>
    /// Includes many child rows per parent using mapped table names and key expressions.
    /// </summary>
    /// <typeparam name="TChild">The child entity or DTO type.</typeparam>
    /// <param name="parentKey">The parent key expression.</param>
    /// <param name="childForeignKey">The child foreign-key expression.</param>
    /// <param name="assign">The assignment action used to attach child rows to the parent.</param>
    /// <returns>The same split graph query for fluent chaining.</returns>
    public ForgeRelationshipSplitQuery<TParent> IncludeMany<TChild>(
        Expression<Func<TParent, object>> parentKey,
        Expression<Func<TChild, object>> childForeignKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
    {
        var parentKeyName = ForgeAstExpression.MemberName(parentKey);
        var childForeignKeyName = ForgeAstExpression.MemberName(childForeignKey);
        return IncludeMany<TChild, object>(
            _ => $"SELECT * FROM {ResolveTableName(typeof(TChild))} WHERE {childForeignKeyName} IN @ParentIds",
            parent => GetValue<object>(parent!, parentKeyName),
            child => GetValue<object>(child!, childForeignKeyName),
            assign);
    }

    /// <summary>
    /// Includes many child rows and assigns them to a writable collection property or existing collection.
    /// </summary>
    /// <typeparam name="TChild">The child entity or DTO type.</typeparam>
    /// <param name="parentKey">The parent key expression.</param>
    /// <param name="childForeignKey">The child foreign-key expression.</param>
    /// <param name="target">The parent collection property that receives the child rows.</param>
    /// <returns>The same split graph query for fluent chaining.</returns>
    public ForgeRelationshipSplitQuery<TParent> IncludeMany<TChild>(
        Expression<Func<TParent, object>> parentKey,
        Expression<Func<TChild, object>> childForeignKey,
        Expression<Func<TParent, IEnumerable<TChild>>> target)
    {
        return IncludeMany(parentKey, childForeignKey, (parent, children) => AssignChildren(parent, children, target, backingField: null));
    }

    /// <summary>
    /// Includes many child rows and assigns them to a private backing field.
    /// </summary>
    /// <typeparam name="TChild">The child entity or DTO type.</typeparam>
    /// <param name="parentKey">The parent key expression.</param>
    /// <param name="childForeignKey">The child foreign-key expression.</param>
    /// <param name="backingField">The parent backing field name that receives the child rows.</param>
    /// <returns>The same split graph query for fluent chaining.</returns>
    public ForgeRelationshipSplitQuery<TParent> IncludeMany<TChild>(
        Expression<Func<TParent, object>> parentKey,
        Expression<Func<TChild, object>> childForeignKey,
        string backingField)
    {
        return IncludeMany(parentKey, childForeignKey, (parent, children) => AssignChildren(parent, children, target: null, backingField));
    }


    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="childSqlFactory">The childSqlFactory value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="assign">The assign value.</param>
    /// <returns>The result of the TKey operation.</returns>
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

            var children = await _db.QueryAsync<TChild>(childSqlFactory(keys), new { Ids = keys, ParentIds = keys }, cancellationToken: ct);
            var lookup = children.GroupBy(childForeignKey).ToDictionary(x => x.Key, x => x.FirstOrDefault());

            foreach (var parent in parents)
            {
                var key = parentKey(parent);
                assign(parent, lookup.TryGetValue(key, out var child) ? child : default);
            }
        });
        return this;
    }

    /// <summary>
    /// Executes the TKey operation.
    /// </summary>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <typeparam name="TKey">The type used by the operation.</typeparam>
    /// <param name="childSqlFactory">The childSqlFactory value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="assign">The assign value.</param>
    /// <returns>The result of the TKey operation.</returns>
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

            var children = await _db.QueryAsync<TChild>(childSqlFactory(keys), new { Ids = keys, ParentIds = keys }, cancellationToken: ct);
            var lookup = children.GroupBy(childForeignKey).ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList());

            foreach (var parent in parents)
            {
                var key = parentKey(parent);
                assign(parent, lookup.TryGetValue(key, out var rows) ? rows : []);
            }
        });
        return this;
    }

    /// <summary>
    /// Executes the TChild operation.
    /// </summary>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <param name="childTable">The childTable value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="childForeignKey">The childForeignKey value.</param>
    /// <param name="target">The target value.</param>
    /// <param name="backingField">The backingField value.</param>
    /// <param name="childWhereSql">The childWhereSql value.</param>
    /// <returns>The result of the TChild operation.</returns>
    public ForgeRelationshipSplitQuery<TParent> IncludeMany<TChild>(
        string childTable,
        string parentKey = "Id",
        string childForeignKey = "ParentId",
        Expression<Func<TParent, IEnumerable<TChild>>>? target = null,
        string? backingField = null,
        string? childWhereSql = null)
    {
        var parentKeyProperty = FindProperty(typeof(TParent), parentKey);
        var childForeignKeyProperty = FindProperty(typeof(TChild), childForeignKey);

        _loaders.Add(async (parents, ct) =>
        {
            var keys = parents.Select(x => parentKeyProperty.GetValue(x)).Where(x => x is not null).Distinct().ToList();
            if (keys.Count == 0) return;

            var sql = $"SELECT * FROM {childTable} WHERE {childForeignKey} IN @ParentIds";
            if (!string.IsNullOrWhiteSpace(childWhereSql))
                sql += " AND " + childWhereSql;

            var children = await _db.QueryAsync<TChild>(sql, new { Ids = keys, ParentIds = keys }, cancellationToken: ct);
            var lookup = children.GroupBy(x => childForeignKeyProperty.GetValue(x)).ToDictionary(x => x.Key, x => (IReadOnlyList<TChild>)x.ToList());

            foreach (var parent in parents)
            {
                var key = parentKeyProperty.GetValue(parent);
                var rows = key is not null && lookup.TryGetValue(key, out var found) ? found : Array.Empty<TChild>();
                AssignChildren(parent, rows, target, backingField);
            }
        });

        return this;
    }


    /// <summary>
    /// Includes many child rows through a join table using mapped table names and key expressions.
    /// </summary>
    /// <typeparam name="TJoin">The join entity or DTO type.</typeparam>
    /// <typeparam name="TChild">The child entity or DTO type.</typeparam>
    /// <param name="parentKey">The parent key expression.</param>
    /// <param name="joinParentKey">The join-table parent key expression.</param>
    /// <param name="joinChildKey">The join-table child key expression.</param>
    /// <param name="childKey">The child key expression.</param>
    /// <param name="assign">The assignment action used to attach child rows to the parent.</param>
    /// <returns>The same split graph query for fluent chaining.</returns>
    public ForgeRelationshipSplitQuery<TParent> IncludeManyToMany<TJoin, TChild>(
        Expression<Func<TParent, object>> parentKey,
        Expression<Func<TJoin, object>> joinParentKey,
        Expression<Func<TJoin, object>> joinChildKey,
        Expression<Func<TChild, object>> childKey,
        Action<TParent, IReadOnlyList<TChild>> assign)
    {
        var parentKeyName = ForgeAstExpression.MemberName(parentKey);
        var joinParentKeyName = ForgeAstExpression.MemberName(joinParentKey);
        var joinChildKeyName = ForgeAstExpression.MemberName(joinChildKey);
        var childKeyName = ForgeAstExpression.MemberName(childKey);

        return IncludeManyToMany<TJoin, TChild, object, object>(
            _ => $"SELECT * FROM {ResolveTableName(typeof(TJoin))} WHERE {joinParentKeyName} IN @ParentIds",
            _ => $"SELECT * FROM {ResolveTableName(typeof(TChild))} WHERE {childKeyName} IN @ChildIds",
            parent => GetValue<object>(parent!, parentKeyName),
            join => GetValue<object>(join!, joinParentKeyName),
            join => GetValue<object>(join!, joinChildKeyName),
            child => GetValue<object>(child!, childKeyName),
            assign);
    }

    /// <summary>
    /// Executes the TChildKey operation.
    /// </summary>
    /// <typeparam name="TJoin">The type used by the operation.</typeparam>
    /// <typeparam name="TChild">The type used by the operation.</typeparam>
    /// <typeparam name="TParentKey">The type used by the operation.</typeparam>
    /// <typeparam name="TChildKey">The type used by the operation.</typeparam>
    /// <param name="joinSqlFactory">The joinSqlFactory value.</param>
    /// <param name="childSqlFactory">The childSqlFactory value.</param>
    /// <param name="parentKey">The parentKey value.</param>
    /// <param name="joinParentKey">The joinParentKey value.</param>
    /// <param name="joinChildKey">The joinChildKey value.</param>
    /// <param name="childKey">The childKey value.</param>
    /// <param name="assign">The assign value.</param>
    /// <returns>The result of the TChildKey operation.</returns>
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

            var joins = await _db.QueryAsync<TJoin>(joinSqlFactory(parentKeys), new { Ids = parentKeys, ParentIds = parentKeys }, cancellationToken: ct);
            var childKeys = joins.Select(joinChildKey).Distinct().ToList();

            if (childKeys.Count == 0)
            {
                foreach (var parent in parents) assign(parent, []);
                return;
            }

            var children = await _db.QueryAsync<TChild>(childSqlFactory(childKeys), new { Ids = childKeys, ChildIds = childKeys }, cancellationToken: ct);
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

    /// <summary>
    /// Executes the Any operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the Any operation.</returns>
    public bool Any(string parentSql, object? parameters = null)
        => _db.ExecuteScalar<int>($"SELECT CASE WHEN EXISTS ({parentSql}) THEN 1 ELSE 0 END", parameters) > 0;

    /// <summary>
    /// Executes the AnyAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the AnyAsync operation.</returns>
    public async ValueTask<bool> AnyAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
        => await _db.ExecuteScalarAsync<int>($"SELECT CASE WHEN EXISTS ({parentSql}) THEN 1 ELSE 0 END", parameters, cancellationToken: cancellationToken) > 0;

    /// <summary>
    /// Executes the FirstOrDefault operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the FirstOrDefault operation.</returns>
    public TParent? FirstOrDefault(string parentSql, object? parameters = null)
        => FirstOrDefaultAsync(parentSql, parameters).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the FirstOrDefaultAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the FirstOrDefaultAsync operation.</returns>
    public async ValueTask<TParent?> FirstOrDefaultAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
        => (await ToListAsync(parentSql, parameters, cancellationToken)).FirstOrDefault();

    /// <summary>
    /// Executes the graph query using the configured parent source.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>The loaded parent rows with configured children attached.</returns>
    public ValueTask<IReadOnlyList<TParent>> ToListAsync(CancellationToken cancellationToken = default)
        => ToListAsync(_parentSql ?? $"SELECT * FROM {ResolveTableName(typeof(TParent))}", _parentParameters, cancellationToken);

    /// <summary>
    /// Executes the graph query and projects each parent into another result shape.
    /// </summary>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="projection">The projection function applied after child rows are loaded.</param>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>The projected result rows.</returns>
    public async ValueTask<IReadOnlyList<TResult>> ToListAsync<TResult>(Func<TParent, TResult> projection, CancellationToken cancellationToken = default)
    {
        var parents = await ToListAsync(cancellationToken);
        return parents.Select(projection).ToList();
    }

    /// <summary>
    /// Executes the graph query using the configured parent source and returns the first row or null.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>The first loaded parent row, or null when no row exists.</returns>
    public async ValueTask<TParent?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
        => (await ToListAsync(cancellationToken)).FirstOrDefault();

    /// <summary>
    /// Executes the graph query and projects the first parent into another result shape.
    /// </summary>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="projection">The projection function applied after child rows are loaded.</param>
    /// <param name="cancellationToken">The cancellation token for the async operation.</param>
    /// <returns>The projected first row, or default when no row exists.</returns>
    public async ValueTask<TResult?> FirstOrDefaultAsync<TResult>(Func<TParent, TResult> projection, CancellationToken cancellationToken = default)
    {
        var parent = await FirstOrDefaultAsync(cancellationToken);
        return parent is null ? default : projection(parent);
    }

    /// <summary>
    /// Executes the ToList operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <returns>The result of the ToList operation.</returns>
    public IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null) => ToListAsync(parentSql, parameters).GetAwaiter().GetResult();

    /// <summary>
    /// Executes the ToListAsync operation.
    /// </summary>
    /// <param name="parentSql">The parentSql value.</param>
    /// <param name="parameters">The parameters value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ToListAsync operation.</returns>
    public async ValueTask<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var parents = (await _db.QueryAsync<TParent>(parentSql, parameters, cancellationToken: cancellationToken)).ToList();
        foreach (var loader in _loaders) await loader(parents, cancellationToken);
        return parents;
    }

    private static string ResolveTableName(Type type)
    {
        var attr = type.GetCustomAttributes(typeof(ForgeTableAttribute), false).Cast<ForgeTableAttribute>().FirstOrDefault();
        return attr?.Name ?? type.Name;
    }

    private static TValue GetValue<TValue>(object instance, string propertyName)
    {
        var value = FindProperty(instance.GetType(), propertyName).GetValue(instance);
        return value is TValue typed ? typed : (TValue)value!;
    }

    private static PropertyInfo FindProperty(Type type, string name)
        => type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
           ?? throw new InvalidOperationException($"Property '{name}' was not found on {type.Name}.");

    private static void AssignChildren<TChild>(
        TParent parent,
        IReadOnlyList<TChild> children,
        Expression<Func<TParent, IEnumerable<TChild>>>? target,
        string? backingField)
    {
        if (!string.IsNullOrWhiteSpace(backingField))
        {
            var field = typeof(TParent).GetField(backingField, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException($"Backing field '{backingField}' was not found on {typeof(TParent).Name}.");

            if (field.GetValue(parent) is IList<TChild> list)
            {
                list.Clear();
                foreach (var child in children) list.Add(child);
                return;
            }

            field.SetValue(parent, children.ToList());
            return;
        }

        if (target is null) return;
        var member = target.Body as MemberExpression;
        if (member?.Member is not PropertyInfo property)
            throw new InvalidOperationException("Target must be a collection property expression, for example x => x.Items.");

        if (property.CanWrite)
        {
            property.SetValue(parent, ConvertChildren(children, property.PropertyType));
            return;
        }

        if (property.GetValue(parent) is IList<TChild> existing)
        {
            existing.Clear();
            foreach (var child in children) existing.Add(child);
        }
    }

    private static object ConvertChildren<TChild>(IReadOnlyList<TChild> children, Type targetType)
    {
        if (targetType.IsAssignableFrom(children.GetType())) return children;
        if (targetType.IsAssignableFrom(typeof(List<TChild>))) return children.ToList();
        if (targetType.IsArray) return children.ToArray();
        return children.ToList();
    }
}
