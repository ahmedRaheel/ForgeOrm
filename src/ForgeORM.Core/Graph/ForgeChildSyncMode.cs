namespace ForgeORM.Core.Graph;

/// <summary>
/// Defines how child collections are synchronized during graph update operations.
/// </summary>
public enum ForgeChildSyncMode
{
    /// <summary>
    /// Only new child rows are inserted.
    /// </summary>
    InsertOnly,

    /// <summary>
    /// Existing child rows are updated and new child rows are inserted.
    /// </summary>
    InsertUpdate,

    /// <summary>
    /// Existing child rows are updated, new child rows are inserted, and missing child rows are deleted.
    /// </summary>
    InsertUpdateDeleteMissing,

    /// <summary>
    /// Existing child rows are deleted and replaced with the supplied child collection.
    /// </summary>
    ReplaceAllChildren,

    /// <summary>
    /// Child collections are ignored.
    /// </summary>
    IgnoreChildren
}
