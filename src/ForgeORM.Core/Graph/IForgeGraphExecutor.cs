namespace ForgeORM.Core.Graph;

/// <summary>
/// Executes provider-specific graph persistence operations.
/// </summary>
public interface IForgeGraphExecutor
{
    /// <summary>
    /// Gets the provider handled by this executor.
    /// </summary>
    ForgeDatabaseProvider Provider { get; }

    /// <summary>
    /// Inserts an entity graph and returns execution statistics.
    /// </summary>
    ValueTask<ForgeGraphResult> InsertGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Updates an entity graph and returns execution statistics.
    /// </summary>
    ValueTask<ForgeGraphResult> UpdateGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Deletes an entity graph and returns execution statistics.
    /// </summary>
    ValueTask<ForgeGraphResult> DeleteGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class;
}
