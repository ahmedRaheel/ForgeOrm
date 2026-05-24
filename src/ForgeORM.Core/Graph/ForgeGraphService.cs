namespace ForgeORM.Core.Graph;

/// <summary>
/// Routes graph operations to the provider-specific graph executor.
/// </summary>
public sealed class ForgeGraphService
{
    private readonly IReadOnlyDictionary<ForgeDatabaseProvider, IForgeGraphExecutor> _executors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForgeGraphService"/> class.
    /// </summary>
    public ForgeGraphService(IEnumerable<IForgeGraphExecutor> executors)
    {
        _executors = executors.ToDictionary(x => x.Provider);
    }

    /// <summary>
    /// Inserts an entity graph using the selected provider.
    /// </summary>
    public ValueTask<ForgeGraphResult> InsertGraphAsync<T>(
        ForgeDatabaseProvider provider,
        T entity,
        ForgeGraphOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return GetExecutor(provider).InsertGraphAsync(entity, options ?? new ForgeGraphOptions(), cancellationToken);
    }

    /// <summary>
    /// Updates an entity graph using the selected provider.
    /// </summary>
    public ValueTask<ForgeGraphResult> UpdateGraphAsync<T>(
        ForgeDatabaseProvider provider,
        T entity,
        ForgeGraphOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return GetExecutor(provider).UpdateGraphAsync(entity, options ?? new ForgeGraphOptions(), cancellationToken);
    }

    /// <summary>
    /// Deletes an entity graph using the selected provider.
    /// </summary>
    public ValueTask<ForgeGraphResult> DeleteGraphAsync<T>(
        ForgeDatabaseProvider provider,
        T entity,
        ForgeGraphOptions? options = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return GetExecutor(provider).DeleteGraphAsync(entity, options ?? new ForgeGraphOptions(), cancellationToken);
    }

    private IForgeGraphExecutor GetExecutor(ForgeDatabaseProvider provider)
    {
        if (_executors.TryGetValue(provider, out var executor))
        {
            return executor;
        }

        throw new NotSupportedException($"Graph executor is not registered for provider '{provider}'.");
    }
}
