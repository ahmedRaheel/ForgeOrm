namespace ForgeORM.Core.Bulk;

/// <summary>Bulk execution strategy selected by the user.</summary>
public enum ForgeBulkStrategy
{
    SqlDataRecord = 0,
    TableTypeParameter = 1,
    SqlBulkCopy = 2,
    PostgreSqlCopy = 10,
    PostgreSqlTempTable = 11,
    MySqlMultiRow = 20,
    MySqlTempTable = 21,
    OracleArrayBinding = 30,
    OracleMerge = 31
}

/// <summary>Per-operation bulk strategy options.</summary>
public sealed class ForgeBulkOperationOptions
{
    public ForgeBulkStrategy InsertStrategy { get; set; } = ForgeBulkStrategy.SqlDataRecord;
    public ForgeBulkStrategy UpdateStrategy { get; set; } = ForgeBulkStrategy.SqlDataRecord;
    public ForgeBulkStrategy DeleteStrategy { get; set; } = ForgeBulkStrategy.SqlDataRecord;
    public ForgeBulkStrategy GraphUpdateStrategy { get; set; } = ForgeBulkStrategy.SqlDataRecord;
    public bool AutoCreateStructures { get; set; } = true;
    public bool AutoRecreateMismatchedStructures { get; set; } = true;
    public int BatchSize { get; set; } = 5000;
    public int CommandTimeoutSeconds { get; set; } = 0;
}

/// <summary>Global defaults for bulk operations. SQL Server defaults to SqlDataRecord.</summary>
public static class ForgeBulkOperationDefaults
{
    private static ForgeBulkOperationOptions _current = new();
    public static ForgeBulkOperationOptions Current => _current;

    public static ForgeBulkOperationOptions Create(Action<ForgeBulkOperationOptions>? configure = null)
    {
        var options = new ForgeBulkOperationOptions
        {
            InsertStrategy = _current.InsertStrategy,
            UpdateStrategy = _current.UpdateStrategy,
            DeleteStrategy = _current.DeleteStrategy,
            GraphUpdateStrategy = _current.GraphUpdateStrategy,
            AutoCreateStructures = _current.AutoCreateStructures,
            AutoRecreateMismatchedStructures = _current.AutoRecreateMismatchedStructures,
            BatchSize = _current.BatchSize,
            CommandTimeoutSeconds = _current.CommandTimeoutSeconds
        };
        configure?.Invoke(options);
        return options;
    }

    public static void Configure(Action<ForgeBulkOperationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var options = Create();
        configure(options);
        _current = options;
    }
}
