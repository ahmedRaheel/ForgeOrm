namespace ForgeORM.Core.Bulk;

/// <summary>
/// Global defaults for ForgeORM bulk operations.
/// </summary>
public static class ForgeBulkOptionsDefaults
{
    private static ForgeBulkOptions _current = new();

    /// <summary>
    /// Gets the current global bulk options.
    /// </summary>
    public static ForgeBulkOptions Current => _current;

    /// <summary>
    /// Configures global bulk options.
    /// </summary>
    public static void Configure(Action<ForgeBulkOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ForgeBulkOptions
        {
            SqlServerStrategy = _current.SqlServerStrategy,
            AutoCreateTableTypes = _current.AutoCreateTableTypes,
            AutoRecreateMismatchedTableTypes = _current.AutoRecreateMismatchedTableTypes,
            BatchSize = _current.BatchSize,
            CommandTimeoutSeconds = _current.CommandTimeoutSeconds
        };

        configure(options);
        _current = options;
    }
}
