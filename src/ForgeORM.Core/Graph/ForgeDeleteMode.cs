namespace ForgeORM.Core.Graph;

/// <summary>
/// Defines how graph delete operations are performed.
/// </summary>
public enum ForgeDeleteMode
{
    /// <summary>
    /// Rows are physically removed from the database.
    /// </summary>
    HardDelete,

    /// <summary>
    /// Rows are marked as deleted by updating a soft-delete column.
    /// </summary>
    SoftDelete
}
