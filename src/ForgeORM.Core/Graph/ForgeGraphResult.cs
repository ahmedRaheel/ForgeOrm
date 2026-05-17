namespace ForgeORM.Core.Graph;

/// <summary>
/// Represents the result of a graph persistence operation.
/// </summary>
public sealed class ForgeGraphResult
{
    /// <summary>
    /// Gets the total number of rows inserted.
    /// </summary>
    public int TotalInserted { get; init; }

    /// <summary>
    /// Gets the total number of rows updated.
    /// </summary>
    public int TotalUpdated { get; init; }

    /// <summary>
    /// Gets the total number of rows deleted.
    /// </summary>
    public int TotalDeleted { get; init; }

    /// <summary>
    /// Gets the operation duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets row counts by table name.
    /// </summary>
    public IReadOnlyDictionary<string, int> RowsByTable { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Gets the strategy selected by table name.
    /// </summary>
    public IReadOnlyDictionary<string, ForgeBulkStrategy> StrategyByTable { get; init; } = new Dictionary<string, ForgeBulkStrategy>();
}
