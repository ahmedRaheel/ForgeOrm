namespace ForgeORM.Benchmarks;

public static class BenchmarkGateManifest
{
    public static readonly string[] RequiredBenchmarks =
    [
        "QueryOneRow",
        "QueryList",
        "InsertOne",
        "BulkInsert",
        "GraphInsert",
        "GetById",
        "Page",
        "Stream",
        "SplitQueryTvpBatching",
        "ProjectionReader",
        "ProviderDirectGetById",
        "IncludeGraphLoader",
        "RuntimeEmit",
        "SourceGenerated",
        "ProviderDirect"
    ];
}
