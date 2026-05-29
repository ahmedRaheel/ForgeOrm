namespace ForgeORM.Abstractions;

/// <summary>
/// Configures provider-native bulk execution behavior.
/// </summary>
public sealed class ForgeProviderBulkOptions
{
    /// <summary>
    /// SQL Server insert/update/delete strategy. Default is <see cref="ForgeBulkOperationStrategy.SqlBulkCopy" /> for fast append-only inserts.
    /// Update/delete internally use temp-table or TVP paths where SqlBulkCopy cannot update/delete the target directly.
    /// </summary>
    public ForgeBulkOperationStrategy SqlServerStrategy { get; set; } = ForgeBulkOperationStrategy.SqlBulkCopy;

    /// <summary>
    /// PostgreSQL bulk strategy. COPY is preferred for inserts; temp-table strategy is used for update/delete semantics.
    /// </summary>
    public ForgeBulkOperationStrategy PostgreSqlStrategy { get; set; } = ForgeBulkOperationStrategy.PostgreSqlCopy;

    /// <summary>
    /// MySQL bulk strategy. Multi-row insert is preferred for inserts; temp-table strategy is used for update/delete semantics.
    /// </summary>
    public ForgeBulkOperationStrategy MySqlStrategy { get; set; } = ForgeBulkOperationStrategy.MySqlMultiRowInsert;

    /// <summary>
    /// Oracle bulk strategy. Array binding is preferred for inserts; MERGE is used for update/upsert semantics.
    /// </summary>
    public ForgeBulkOperationStrategy OracleStrategy { get; set; } = ForgeBulkOperationStrategy.OracleArrayBinding;

    /// <summary>
    /// Row count at which SQL Server may switch to SqlBulkCopy for append-only inserts.
    /// </summary>
    public int SqlBulkCopyThreshold { get; set; } = 1;

    /// <summary>
    /// Creates provider-specific helper structures when required, such as SQL Server TVP types or provider temp tables.
    /// </summary>
    public bool AutoCreateProviderStructures { get; set; } = true;

    /// <summary>
    /// Recreates provider helper structures when their shape no longer matches the current entity shape.
    /// </summary>
    public bool AutoRecreateMismatchedStructures { get; set; } = true;

    /// <summary>
    /// Drops provider helper structures after each operation. Keep false for performance; set true only for throwaway/test databases.
    /// </summary>
    public bool AutoDropProviderStructures { get; set; }

    /// <summary>
    /// Backward-compatible alias for SQL Server TVP creation.
    /// </summary>
    public bool AutoCreateTableTypes
    {
        get => AutoCreateProviderStructures;
        set => AutoCreateProviderStructures = value;
    }

    /// <summary>
    /// Maximum rows per batch for provider fallback paths.
    /// </summary>
    public int BatchSize { get; set; } = 5_000;

    /// <summary>
    /// Command timeout in seconds. Zero means provider default.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; }
}
