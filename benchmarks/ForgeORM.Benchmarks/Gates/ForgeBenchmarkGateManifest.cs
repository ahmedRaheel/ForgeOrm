namespace ForgeORM.Benchmarks.Gates;

public static class ForgeBenchmarkGateManifest
{
    public static readonly string[] RequiredBenchmarks =
    [
        "query-one-row",
        "query-list",
        "insert-one",
        "bulk-insert",
        "graph-insert",
        "get-by-id",
        "page",
        "stream",
        "SplitQueryTvpBatching",
        "ProjectionReader",
        "ProviderDirectGetById",
        "IncludeGraphLoader",
        "RuntimeEmit",
        "SourceGenerated",
        "ProviderDirect"
    ];

    public const double QueryRegressionLimitPercent = 5.0;
    public const double BulkRegressionLimitPercent = 10.0;
    public const double GraphRegressionLimitPercent = 10.0;
}

// Added by source-generator/native-bulk patch: benchmark records/structs/enums/composite keys/projections/includes/provider bulk.
