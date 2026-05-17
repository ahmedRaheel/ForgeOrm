namespace ForgeORM.Core.Graph;

/// <summary>
/// Graph persistence options.
/// </summary>
public sealed class ForgeGraphOptions
{
    /// <summary>
    /// Gets or sets whether child collections should be included.
    /// </summary>
    public bool IncludeChildren { get; set; } = true;

    /// <summary>
    /// Gets or sets whether graph operations should run inside a transaction.
    /// </summary>
    public bool UseTransaction { get; set; } = true;

    /// <summary>
    /// Gets or sets whether generated keys should be returned and propagated to children.
    /// </summary>
    public bool ReturnGeneratedKeys { get; set; } = true;

    /// <summary>
    /// Gets or sets whether provider bulk paths should be used when possible.
    /// </summary>
    public bool UseBulkWhenPossible { get; set; } = true;

    /// <summary>
    /// Gets or sets the preferred batch size.
    /// </summary>
    public int BatchSize { get; set; } = 500;

    /// <summary>
    /// Gets or sets the maximum graph traversal depth.
    /// </summary>
    public int MaxDepth { get; set; } = 5;

    /// <summary>
    /// Gets or sets the requested strategy. Auto lets ForgeORM choose the fastest provider path.
    /// </summary>
    public ForgeBulkStrategy Strategy { get; set; } = ForgeBulkStrategy.Auto;

    /// <summary>
    /// Gets or sets how child collections should be synchronized during graph updates.
    /// </summary>
    public ForgeChildSyncMode ChildSyncMode { get; set; } = ForgeChildSyncMode.InsertUpdateDeleteMissing;

    /// <summary>
    /// Gets or sets how graph delete operations should remove rows.
    /// </summary>
    public ForgeDeleteMode DeleteMode { get; set; } = ForgeDeleteMode.HardDelete;

    /// <summary>
    /// Gets or sets the soft-delete column name.
    /// </summary>
    public string SoftDeleteColumn { get; set; } = "IsDeleted";

    /// <summary>
    /// Gets or sets the soft-delete timestamp column name.
    /// </summary>
    public string? SoftDeleteTimestampColumn { get; set; } = "DeletedAt";

    /// <summary>
    /// Configures the operation to use bulk strategies when available.
    /// </summary>
    public ForgeGraphOptions UseBulk()
    {
        UseBulkWhenPossible = true;
        Strategy = ForgeBulkStrategy.Auto;
        return this;
    }

    /// <summary>
    /// Configures the operation to synchronize child collections.
    /// </summary>
    public ForgeGraphOptions SyncChildren(ForgeChildSyncMode mode = ForgeChildSyncMode.InsertUpdateDeleteMissing)
    {
        IncludeChildren = true;
        ChildSyncMode = mode;
        return this;
    }

    /// <summary>
    /// Configures the operation to ignore child collections.
    /// </summary>
    public ForgeGraphOptions IgnoreChildren()
    {
        IncludeChildren = false;
        ChildSyncMode = ForgeChildSyncMode.IgnoreChildren;
        return this;
    }

    /// <summary>
    /// Configures soft delete for graph delete operations.
    /// </summary>
    public ForgeGraphOptions SoftDelete(string columnName = "IsDeleted")
    {
        DeleteMode = ForgeDeleteMode.SoftDelete;
        SoftDeleteColumn = columnName;
        return this;
    }

    /// <summary>
    /// Configures hard delete for graph delete operations.
    /// </summary>
    public ForgeGraphOptions HardDelete()
    {
        DeleteMode = ForgeDeleteMode.HardDelete;
        return this;
    }
}
