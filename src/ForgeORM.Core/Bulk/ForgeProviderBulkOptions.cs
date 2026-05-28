namespace ForgeORM.Core.Bulk;

/// <summary>
/// Bulk operation options used by ForgeORM provider bulk routers.
/// </summary>
public sealed class ForgeProviderBulkOptions
{
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
}

/// <summary>
/// Global default bulk options.
/// </summary>
public static class ForgeProviderBulkOptionsDefaults
{
    private static ForgeProviderBulkOptions _current = new();

    public static ForgeProviderBulkOptions Current => _current;

    public static void Configure(Action<ForgeProviderBulkOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var next = new ForgeProviderBulkOptions
        {
            SqlServerStrategy = _current.SqlServerStrategy,
            PostgreSqlStrategy = _current.PostgreSqlStrategy,
            MySqlStrategy = _current.MySqlStrategy,
            OracleStrategy = _current.OracleStrategy,
            SqlBulkCopyThreshold = _current.SqlBulkCopyThreshold,
            AutoCreateProviderStructures = _current.AutoCreateProviderStructures,
            AutoRecreateMismatchedStructures = _current.AutoRecreateMismatchedStructures,
            BatchSize = _current.BatchSize,
            CommandTimeoutSeconds = _current.CommandTimeoutSeconds
        };

        configure(next);
        _current = next;
    }
}
