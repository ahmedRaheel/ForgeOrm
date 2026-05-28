namespace ForgeORM.Core.Bulk;

/// <summary>
/// SQL Server bulk execution strategy.
/// Default is <see cref="SqlDataRecord"/>.
/// </summary>
public enum ForgeSqlServerBulkStrategy
{
    /// <summary>
    /// Uses SqlDataRecord with a SQL Server table-valued parameter.
    /// This is the default and primary high-performance path.
    /// </summary>
    SqlDataRecord = 0,

    /// <summary>
    /// Uses DataTable as a SQL Server structured table-valued parameter.
    /// This is the safe fallback path when SqlDataRecord is unavailable.
    /// </summary>
    TableTypeParameter = 1,

    /// <summary>
    /// Uses SqlBulkCopy for append-only bulk inserts.
    /// Update/delete operations still use table-valued parameter strategies.
    /// </summary>
    SqlBulkCopy = 2
}
