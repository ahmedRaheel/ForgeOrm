using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Central enterprise feature registry.
/// </summary>
public static class ForgeEnterpriseFeatureRegistry
{
    private static readonly IReadOnlyDictionary<ForgeEnterpriseFeature, ForgeEnterpriseFeatureResult> Items =
        new Dictionary<ForgeEnterpriseFeature, ForgeEnterpriseFeatureResult>
        {
            [ForgeEnterpriseFeature.DistributedQueryExecution] = Result("Distributed query execution", "Shard routing, cross-shard union, tenant shard resolver, read/write split.", ["UseShard", "UnionShards", "UseReadReplica", "TenantShardResolver"]),
            [ForgeEnterpriseFeature.DistributedCache] = Result("Distributed cache integration", "Hybrid distributed cache contracts for Redis/NCache/FusionCache-style providers.", ["CacheDistributed", "HybridCache", "CacheInvalidation", "TenantAwareCacheKeys"]),
            [ForgeEnterpriseFeature.QueryPlanAnalysis] = Result("Query plan analysis engine", "Explain/analyze API for missing indexes, scans, sorts and joins.", ["ExplainAsync", "AnalyzePlan", "MissingIndexHints", "SortWarnings"]),
            [ForgeEnterpriseFeature.AutomaticQueryOptimization] = Result("Automatic query optimization", "Rule-based query rewrite recommendations.", ["AutoOptimize", "OffsetToKeyset", "ProjectionTrim", "SplitQuerySuggestion"]),
            [ForgeEnterpriseFeature.AdaptiveQueryExecution] = Result("Adaptive query execution", "Chooses streaming, caching, snapshot reads and keyset paging based on query shape.", ["AdaptiveExecute", "EstimatedRowsPolicy", "DashboardCachePolicy"]),
            [ForgeEnterpriseFeature.AsyncStreaming] = Result("Real async streaming", "Low-allocation IAsyncEnumerable streaming foundation.", ["StreamAsync", "SequentialAccess", "Backpressure", "Cancellation"]),
            [ForgeEnterpriseFeature.ColumnarAnalytics] = Result("Columnar analytics engine", "Column-oriented analytics contracts and vectorized aggregation extension points.", ["ColumnStoreFrame", "VectorizedSum", "ParquetExtension", "ArrowExtension"]),
            [ForgeEnterpriseFeature.MaterializedQueryCache] = Result("Materialized query cache", "Named dashboard/report cache with refresh metadata.", ["Materialize", "Refresh", "Invalidate", "ScheduleRefresh"]),
            [ForgeEnterpriseFeature.ChangeTrackingEventSourcing] = Result("Change tracking/event sourcing hybrid", "Property diffs, audit snapshots and event stream contracts.", ["TrackChanges", "Diff", "Snapshot", "EventStream"]),
            [ForgeEnterpriseFeature.DatabaseObservability] = Result("Database observability", "Slow queries, lock waits, deadlocks, pool and cache metrics.", ["SlowQueryLog", "LockWaitMetric", "DeadlockMetric", "TenantMetrics"]),
            [ForgeEnterpriseFeature.OpenTelemetryIntegration] = Result("Native OpenTelemetry integration", "Trace/metric bridge for ForgeORM query execution.", ["ActivitySource", "Metrics", "TenantTags", "DbAttributes"]),
            [ForgeEnterpriseFeature.SourceGenerators] = Result("Source generators", "Generated SQL/mappers/readers/graph plans extension model.", ["CompiledReaders", "CompiledMappings", "CompiledGraphPlans", "GeneratedSql"]),
            [ForgeEnterpriseFeature.BinaryProtocolOptimizations] = Result("Binary protocol optimizations", "Provider binary import and prepared statement optimization contracts.", ["NpgsqlBinaryImport", "SqlServerTvpBinary", "PreparedReuse"]),
            [ForgeEnterpriseFeature.AiNativeFeatures] = Result("AI-native features", "Natural language query and AI optimization/schema understanding contracts.", ["NaturalLanguageQuery", "AiIndexAdvice", "GenerateReports", "GenerateDtos"]),
            [ForgeEnterpriseFeature.AdvancedTransactions] = Result("Advanced transaction system", "Saga, retryable transactions, snapshot transactions, idempotency and outbox/inbox.", ["Saga", "RetryableTransaction", "SnapshotTransaction", "Idempotency"]),
            [ForgeEnterpriseFeature.GraphQLIntegration] = Result("GraphQL integration", "GraphQL projection bridge and endpoint foundation.", ["GraphQLProjection", "QueryBridge", "SchemaBridge"]),
            [ForgeEnterpriseFeature.DataVirtualization] = Result("Data virtualization", "Virtual query over DB/API/file sources.", ["VirtualSource", "FederatedQuery", "ApiSource", "FileSource"]),
            [ForgeEnterpriseFeature.TimeSeriesOptimization] = Result("Time-series optimization", "Time buckets, retention, compression and rolling windows.", ["TimeBucket", "RetentionPolicy", "Compression", "WindowAggregation"]),
            [ForgeEnterpriseFeature.EnterpriseMigrationEngine] = Result("Enterprise migration engine", "Schema diff, rollback, zero-downtime migration and online index plans.", ["SchemaDiff", "Rollback", "OnlineIndex", "DataMigrationPlan"]),
            [ForgeEnterpriseFeature.EnterpriseAdminPortal] = Result("Full enterprise admin portal", "Dashboard model for queries, cache, tenants, migrations, outbox and indexes.", ["QueryMonitor", "CacheMonitor", "TenantManager", "MigrationDashboard"])
        };

    public static IReadOnlyList<ForgeEnterpriseFeatureResult> All()
        => Items.Values.ToList();

    public static ForgeEnterpriseFeatureResult Get(ForgeEnterpriseFeature feature)
        => Items[feature];

    private static ForgeEnterpriseFeatureResult Result(string feature, string description, IReadOnlyList<string> capabilities)
        => new(feature, "FoundationReady", description, capabilities);
}
