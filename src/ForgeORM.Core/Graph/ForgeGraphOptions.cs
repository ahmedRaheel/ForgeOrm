namespace ForgeORM.Core.Graph;

/// <summary>
/// Configures graph persistence behavior.
/// </summary>
public sealed class ForgeGraphOptions
{
    /// <summary>
    /// Gets or sets whether child collections should be included.
    /// </summary>
    public bool IncludeChildren { get; set; } = true;

    /// <summary>
    /// Gets or sets whether graph execution should use a transaction.
    /// </summary>
    public bool UseTransaction { get; set; } = true;

    /// <summary>
    /// Gets or sets whether generated database keys should be returned and propagated.
    /// </summary>
    public bool ReturnGeneratedKeys { get; set; } = true;

    /// <summary>
    /// Gets or sets whether provider bulk strategies may be selected automatically.
    /// </summary>
    public bool UseBulkWhenPossible { get; set; } = true;

    /// <summary>
    /// Gets or sets the preferred batch size for provider bulk writers.
    /// </summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum graph traversal depth.
    /// </summary>
    public int MaxDepth { get; set; } = 5;

    /// <summary>
    /// Gets or sets the requested provider strategy. Auto lets ForgeORM choose.
    /// </summary>
    public ForgeBulkStrategy Strategy { get; set; } = ForgeBulkStrategy.Auto;

    /// <summary>
    /// Gets or sets child synchronization behavior for graph updates.
    /// </summary>
    public ForgeChildSyncMode ChildSyncMode { get; set; } = ForgeChildSyncMode.InsertUpdateDeleteMissing;

    /// <summary>
    /// Gets or sets graph delete behavior.
    /// </summary>
    public ForgeDeleteMode DeleteMode { get; set; } = ForgeDeleteMode.HardDelete;

    /// <summary>
    /// Gets or sets the soft-delete flag column name.
    /// </summary>
    public string SoftDeleteColumn { get; set; } = "IsDeleted";
}
