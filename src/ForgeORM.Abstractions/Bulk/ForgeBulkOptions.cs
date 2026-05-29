namespace ForgeORM.Abstractions;

/// <summary>
/// Configures provider bulk execution behavior.
/// </summary>
public sealed class ForgeBulkOptions
{
    /// <summary>
    /// Gets or sets the SQL Server bulk strategy.
    /// Default is SqlDataRecord.
    /// </summary>
    public ForgeSqlServerBulkStrategy SqlServerStrategy { get; set; } =
        ForgeSqlServerBulkStrategy.SqlDataRecord;

    /// <summary>
    /// Gets or sets whether SQL Server TVP table types should be created automatically.
    /// </summary>
    public bool AutoCreateTableTypes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether mismatched SQL Server TVP table types should be dropped and recreated automatically.
    /// </summary>
    public bool AutoRecreateMismatchedTableTypes { get; set; } = true;

    /// <summary>
    /// Gets or sets the bulk batch size.
    /// </summary>
    public int BatchSize { get; set; } = 5000;

    /// <summary>
    /// Gets or sets command timeout seconds. Zero means provider default/infinite depending on provider.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 0;
}
