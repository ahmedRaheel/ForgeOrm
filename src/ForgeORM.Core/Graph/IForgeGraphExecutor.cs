namespace ForgeORM.Core.Graph;

/// <summary>
/// Executes graph persistence operations.
/// </summary>
public interface IForgeGraphExecutor
{
    /// <summary>
    /// Gets the database provider handled by this executor.
    /// </summary>
    ForgeDatabaseProvider Provider { get; }

    /// <summary>
    /// Inserts an entity graph.
    /// </summary>
    Task<ForgeGraphResult> InsertGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Updates an entity graph.
    /// </summary>
    Task<ForgeGraphResult> UpdateGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Deletes an entity graph.
    /// </summary>
    Task<ForgeGraphResult> DeleteGraphAsync<T>(
        T entity,
        ForgeGraphOptions options,
        CancellationToken cancellationToken = default)
        where T : class;
}
