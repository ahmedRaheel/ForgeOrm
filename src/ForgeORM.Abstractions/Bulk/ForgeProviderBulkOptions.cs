namespace ForgeORM.Abstractions;

/// <summary>
/// Provider-native bulk operation options used by ForgeORM bulk routers.
/// </summary>
public sealed class ForgeProviderBulkOptions
{
    /// <summary>
    /// SQL Server insert default uses SqlBulkCopy for the fastest append-only path.
    /// Update and delete route to the matching provider strategy switch and may use temp tables or TVPs.
    /// </summary>
    public ForgeBulkOperationStrategy SqlServerStrategy { get; set; } =
        ForgeBulkOperationStrategy.SqlBulkCopy;

    /// <summary>
    /// PostgreSQL default insert strategy is COPY; update/delete use COPY into a temporary table when available.
    /// </summary>
    public ForgeBulkOperationStrategy PostgreSqlStrategy { get; set; } =
        ForgeBulkOperationStrategy.PostgreSqlCopy;

    /// <summary>
    /// MySQL default insert strategy is multi-row insert; update/delete use temporary table joins when available.
    /// </summary>
    public ForgeBulkOperationStrategy MySqlStrategy { get; set; } =
        ForgeBulkOperationStrategy.MySqlMultiRowInsert;

    /// <summary>
    /// Oracle default insert strategy is array binding; update/delete use MERGE/staging paths when available.
    /// </summary>
    public ForgeBulkOperationStrategy OracleStrategy { get; set; } =
        ForgeBulkOperationStrategy.OracleArrayBinding;

    /// <summary>
    /// Minimum row count at which SQL Server may auto-select SqlBulkCopy for append-only insert.
    /// Set to <see cref="int.MaxValue"/> to disable automatic selection.
    /// </summary>
    public int SqlBulkCopyThreshold { get; set; } = 100_000;

    /// <summary>
    /// Allows providers to create required temporary/staging structures during bulk operations.
    /// </summary>
    public bool AutoCreateProviderStructures { get; set; } = true;

    /// <summary>
    /// Allows providers to recreate incompatible provider structures when shape mismatch is detected.
    /// </summary>
    public bool AutoRecreateMismatchedStructures { get; set; } = true;

    /// <summary>
    /// Drops transient provider structures after each operation. Keep false for stable provider-managed structures.
    /// Temporary tables are always cleaned up by the operation.
    /// </summary>
    public bool AutoDropProviderStructures { get; set; }

    /// <summary>
    /// Backward-compatible alias for SQL Server TVP structure creation.
    /// </summary>
    public bool AutoCreateTableTypes { get; set; } = true;

    /// <summary>
    /// Provider batch size. Providers may cap this based on parameter limits.
    /// </summary>
    public int BatchSize { get; set; } = 5000;

    /// <summary>
    /// Command timeout in seconds. A value of 0 uses provider default/no timeout override.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; }
}
