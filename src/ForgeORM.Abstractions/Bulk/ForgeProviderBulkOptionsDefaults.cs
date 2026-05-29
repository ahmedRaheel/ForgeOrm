namespace ForgeORM.Abstractions;

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
            CommandTimeoutSeconds = _current.CommandTimeoutSeconds,
            AutoDropProviderStructures = _current.AutoDropProviderStructures,
        };

        configure(next);
        _current = next;
    }
}

