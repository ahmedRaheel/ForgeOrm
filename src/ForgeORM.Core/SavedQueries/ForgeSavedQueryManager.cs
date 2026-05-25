namespace ForgeORM.Core.SavedQueries;

/// <summary>
/// Executes reusable saved queries against a ForgeDb instance.
/// </summary>
public sealed class ForgeSavedQueryManager
{
    private readonly ForgeDb _db;
    private readonly ForgeSavedQueryRegistry _registry;

    public ForgeSavedQueryManager(ForgeDb db, ForgeSavedQueryRegistry? registry = null)
    {
        _db = db;
        _registry = registry ?? new ForgeSavedQueryRegistry();
    }

    /// <summary>
    /// Registers or replaces a saved query.
    /// </summary>
    public ForgeSavedQueryManager Register(
        string name,
        string sql,
        object? parameters = null,
        string? description = null)
    {
        _registry.Register(name, sql, parameters, description);
        return this;
    }

    /// <summary>
    /// Registers or replaces a saved query.
    /// </summary>
    public ForgeSavedQueryManager Register(ForgeSavedQuery query)
    {
        _registry.Register(query);
        return this;
    }


    /// <summary>
    /// Registers or replaces a saved query from the expression query builder.
    /// </summary>
    public ValueTask Register(
        string name,
        Action<ForgeSavedQueryBuilderRoot> configure,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Saved query name is required.", nameof(name));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var root = new ForgeSavedQueryBuilderRoot(_db);
        configure(root);
        var rendered = root.Render();
        _registry.Register(name, rendered.Sql, rendered.Parameters, description);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Registers or replaces a saved query from the typed expression query builder.
    /// </summary>
    public ValueTask Register<TEntity>(
        string name,
        Action<ForgeQueryBuilder<TEntity>> configure,
        string? description = null)
        where TEntity : class, new()
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Saved query name is required.", nameof(name));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = _db.Query<TEntity>();
        configure(builder);
        var rendered = builder.Render();
        _registry.Register(name, rendered.Sql, rendered.Parameters, description);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Lists saved query definitions.
    /// </summary>
    public IReadOnlyList<ForgeSavedQuery> List() => _registry.List();

    /// <summary>
    /// Gets a saved query definition.
    /// </summary>
    public ForgeSavedQuery Get(string name) => _registry.Get(name);

    /// <summary>
    /// Removes a saved query definition.
    /// </summary>
    public bool Remove(string name) => _registry.Remove(name);

    /// <summary>
    /// Executes a saved query and materializes typed rows.
    /// </summary>
    public ValueTask<IReadOnlyList<T>> ExecuteAsync<T>(
        string name,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var query = _registry.Get(name);
        return _db.QueryAsync<T>(
            query.Sql,
            parameters ?? query.Parameters,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes a saved query and returns the first row or null.
    /// </summary>
    public async ValueTask<T?> ExecuteSingleOrDefaultAsync<T>(
        string name,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var rows = await ExecuteAsync<T>(name, parameters, cancellationToken);
        return rows.FirstOrDefault();
    }
}
