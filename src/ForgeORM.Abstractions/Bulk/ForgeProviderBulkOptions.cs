namespace ForgeORM.Abstractions;

/// <summary>
/// Bulk operation options used by ForgeORM provider bulk routers.
/// </summary>
public sealed class ForgeProviderBulkOptions
{
    public ForgeProviderBulkOptions DefaultBulkOptions { get; set; }
       = ForgeProviderBulkOptionsDefaults.Current;
    /// <summary>
    /// SQL Server default is SqlDataRecord.
    /// </summary>
    public ForgeBulkOperationStrategy SqlServerStrategy { get; set; } =
        ForgeBulkOperationStrategy.SqlDataRecord;

    /// <summary>
    /// PostgreSQL default is COPY.
    /// </summary>
    public ForgeBulkOperationStrategy PostgreSqlStrategy { get; set; } =
        ForgeBulkOperationStrategy.PostgreSqlCopy;

    /// <summary>
    /// MySQL default is multi-row insert / temp-table update-delete.
    /// </summary>
    public ForgeBulkOperationStrategy MySqlStrategy { get; set; } =
        ForgeBulkOperationStrategy.MySqlMultiRowInsert;

    /// <summary>
    /// Oracle default is array binding.
    /// </summary>
    public ForgeBulkOperationStrategy OracleStrategy { get; set; } =
        ForgeBulkOperationStrategy.OracleArrayBinding;

    /// <summary>
    /// Use SqlBulkCopy for SQL Server append-only insert when row count reaches this threshold.
    /// Set to int.MaxValue to never auto-select SqlBulkCopy.
    /// </summary>
    public int SqlBulkCopyThreshold { get; set; } = 100_000;

    public bool AutoCreateProviderStructures { get; set; } = true;
    public bool AutoRecreateMismatchedStructures { get; set; } = true;
    public int BatchSize { get; set; } = 5000;
    public int CommandTimeoutSeconds { get; set; } = 0;
    public bool AutoCreateTableTypes { get; set; } = true;
}

