using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Enterprise feature identifiers for advanced ForgeORM modules.
/// </summary>
public enum ForgeEnterpriseFeature
{
    DistributedQueryExecution,
    DistributedCache,
    QueryPlanAnalysis,
    AutomaticQueryOptimization,
    AdaptiveQueryExecution,
    AsyncStreaming,
    ColumnarAnalytics,
    MaterializedQueryCache,
    ChangeTrackingEventSourcing,
    DatabaseObservability,
    OpenTelemetryIntegration,
    RuntimeEmitMaterializers,
    BinaryProtocolOptimizations,
    AiNativeFeatures,
    AdvancedTransactions,
    GraphQLIntegration,
    DataVirtualization,
    TimeSeriesOptimization,
    EnterpriseMigrationEngine,
    EnterpriseAdminPortal
}

/// <summary>
/// Generic response returned by enterprise feature samples.
/// </summary>
public sealed record ForgeEnterpriseFeatureResult(
    string Feature,
    string Status,
    string Description,
    IReadOnlyList<string> Capabilities,
    object? Data = null);

/// <summary>
/// Distributed shard descriptor.
/// </summary>
public sealed record ForgeShardDescriptor(
    string Name,
    string ConnectionString,
    bool IsReadReplica = false,
    string? Region = null,
    string? TenantId = null);

/// <summary>
/// Query plan warning returned by the explain/analyze engine.
/// </summary>
public sealed record ForgeQueryPlanWarning(
    string Code,
    string Severity,
    string Message);

/// <summary>
/// Query plan analysis result.
/// </summary>
public sealed record ForgeQueryPlanAnalysisResult(
    string Sql,
    IReadOnlyList<ForgeQueryPlanWarning> Warnings,
    IReadOnlyList<string> SuggestedIndexes,
    IReadOnlyList<string> OptimizationHints);

/// <summary>
/// Adaptive execution recommendation.
/// </summary>
public sealed record ForgeAdaptiveExecutionPlan(
    string Mode,
    bool UseStreaming,
    bool UseCache,
    bool UseKeysetPaging,
    bool UseReadReplica,
    string Reason);

/// <summary>
/// Materialized query cache entry.
/// </summary>
public sealed record ForgeMaterializedQuery(
    string Name,
    string Sql,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastRefreshUtc,
    TimeSpan RefreshInterval);

/// <summary>
/// Field-level change record.
/// </summary>
public sealed record ForgeEntityChange(
    string Entity,
    string Property,
    object? OldValue,
    object? NewValue,
    DateTimeOffset ChangedAtUtc);

/// <summary>
/// Observability metric snapshot.
/// </summary>
public sealed record ForgeDatabaseMetric(
    string Name,
    double Value,
    string Unit,
    DateTimeOffset CapturedAtUtc);

/// <summary>
/// Migration operation produced by schema diff.
/// </summary>
public sealed record ForgeMigrationOperation(
    string Operation,
    string Target,
    string Sql);

/// <summary>
/// Virtual data source descriptor.
/// </summary>
public sealed record ForgeVirtualDataSource(
    string Name,
    string Kind,
    string ConnectionOrEndpoint);

/// <summary>
/// Time series bucket definition.
/// </summary>
public sealed record ForgeTimeBucket(
    string Unit,
    int Size);

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
            [ForgeEnterpriseFeature.RuntimeEmitMaterializers] = Result("RuntimeEmit materializers", "MSIL DynamicMethod readers, binders and direct SQL Server hot paths.", ["CompiledReaders", "CompiledBinders", "DirectSqlServerReaders", "NoReflectionHotPath"]),
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

/// <summary>
/// Shard router foundation.
/// </summary>
public sealed class ForgeShardRouter
{
    private readonly ConcurrentDictionary<string, ForgeShardDescriptor> _shards = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ForgeShardDescriptor shard)
        => _shards[shard.Name] = shard;

    public IReadOnlyList<ForgeShardDescriptor> All()
        => _shards.Values.OrderBy(x => x.Name).ToList();

    public ForgeShardDescriptor? Resolve(string name)
        => _shards.TryGetValue(name, out var shard) ? shard : null;
}

/// <summary>
/// Distributed cache abstraction foundation.
/// </summary>
public interface IForgeDistributedCache
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory distributed-cache-compatible implementation for samples.
/// </summary>
public sealed class InMemoryForgeDistributedCache : IForgeDistributedCache
{
    private sealed record Entry(object? Value, DateTimeOffset ExpiresAtUtc);
    private readonly ConcurrentDictionary<string, Entry> _items = new(StringComparer.OrdinalIgnoreCase);

    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_items.TryGetValue(key, out var entry) || entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            _items.TryRemove(key, out _);
            return ValueTask.FromResult(default(T));
        }

        return ValueTask.FromResult((T?)entry.Value);
    }

    public ValueTask SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _items[key] = new Entry(value, DateTimeOffset.UtcNow.Add(ttl));
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _items.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Query plan analyzer foundation.
/// </summary>
public static class ForgeQueryPlanAnalyzer
{
    public static ForgeQueryPlanAnalysisResult Analyze(string sql)
    {
        var warnings = new List<ForgeQueryPlanWarning>();
        var hints = new List<string>();

        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new("SELECT_STAR", "Medium", "Avoid SELECT * in large enterprise queries."));
            hints.Add("Project only required columns.");
        }

        if (sql.Contains("OFFSET", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new("OFFSET_PAGING", "High", "OFFSET can become slow for millions of rows."));
            hints.Add("Use keyset pagination for large datasets.");
        }

        if (!sql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add(new("NO_FILTER", "Medium", "Query has no WHERE clause."));
            hints.Add("Add filters or pagination before using this query in production.");
        }

        return new ForgeQueryPlanAnalysisResult(
            sql,
            warnings,
            ["Review WHERE + ORDER BY columns for composite indexes."],
            hints.Count == 0 ? ["No major optimization warnings detected."] : hints);
    }
}

/// <summary>
/// Automatic query optimizer foundation.
/// </summary>
public static class ForgeAutomaticQueryOptimizer
{
    public static string Optimize(string sql)
    {
        if (sql.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
        {
            return sql + "\n-- ForgeORM suggestion: replace SELECT * with explicit projection.";
        }

        return sql + "\n-- ForgeORM: no automatic rewrite applied.";
    }
}

/// <summary>
/// Adaptive execution planner foundation.
/// </summary>
public static class ForgeAdaptiveExecutionPlanner
{
    public static ForgeAdaptiveExecutionPlan Plan(string sql, int? estimatedRows = null)
    {
        if (estimatedRows >= 1_000_000)
        {
            return new("LargeDataStreaming", true, false, true, true, "Estimated rows exceed million-record threshold.");
        }

        if (sql.Contains("GROUP BY", StringComparison.OrdinalIgnoreCase))
        {
            return new("DashboardCachedSnapshot", false, true, false, true, "Aggregate/report query is cache-friendly.");
        }

        return new("Standard", false, false, false, false, "Standard execution is acceptable.");
    }
}

/// <summary>
/// Streaming helpers for large in-memory sequences and sample integration.
/// </summary>
public static class ForgeStreamingExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncStream<T>(
        this IEnumerable<T> rows,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
            await Task.Yield();
        }
    }

    public static async ValueTask ProcessInBatchesAsync<T>(
        this IAsyncEnumerable<T> rows,
        int batchSize,
        Func<IReadOnlyList<T>, ValueTask> processor,
        CancellationToken cancellationToken = default)
    {
        var batch = new List<T>(batchSize);

        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            batch.Add(row);

            if (batch.Count >= batchSize)
            {
                await processor(batch);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await processor(batch);
        }
    }
}

/// <summary>
/// Columnar analytics frame foundation.
/// </summary>
public sealed class ForgeColumnarFrame
{
    private readonly Dictionary<string, List<object?>> _columns = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, IReadOnlyList<object?>> Columns =>
        _columns.ToDictionary(x => x.Key, x => (IReadOnlyList<object?>)x.Value, StringComparer.OrdinalIgnoreCase);

    public int RowCount => _columns.Count == 0 ? 0 : _columns.Values.Max(x => x.Count);

    public void AddColumn(string name, IEnumerable<object?> values)
        => _columns[name] = values.ToList();

    public decimal Sum(string column)
        => _columns.TryGetValue(column, out var values)
            ? values.Where(x => x is not null).Sum(x => Convert.ToDecimal(x))
            : 0m;
}

/// <summary>
/// Materialized query cache manager foundation.
/// </summary>
public sealed class ForgeMaterializedQueryCache
{
    private readonly ConcurrentDictionary<string, ForgeMaterializedQuery> _queries = new(StringComparer.OrdinalIgnoreCase);

    public ForgeMaterializedQuery Register(string name, string sql, TimeSpan refreshInterval)
    {
        var query = new ForgeMaterializedQuery(name, sql, DateTimeOffset.UtcNow, null, refreshInterval);
        _queries[name] = query;
        return query;
    }

    public IReadOnlyList<ForgeMaterializedQuery> All() => _queries.Values.ToList();

    public bool Invalidate(string name) => _queries.TryRemove(name, out _);
}

/// <summary>
/// Change tracker/event sourcing helper foundation.
/// </summary>
public static class ForgeChangeTracker
{
    public static IReadOnlyList<ForgeEntityChange> Diff<T>(T before, T after)
    {
        if (before is null || after is null) return [];

        var changes = new List<ForgeEntityChange>();
        var type = typeof(T);

        foreach (var prop in type.GetProperties().Where(p => p.CanRead))
        {
            var oldValue = ForgeRuntimeAccessorCache.Get(prop, before);
            var newValue = ForgeRuntimeAccessorCache.Get(prop, after);

            if (!Equals(oldValue, newValue))
            {
                changes.Add(new ForgeEntityChange(type.Name, prop.Name, oldValue, newValue, DateTimeOffset.UtcNow));
            }
        }

        return changes;
    }
}

/// <summary>
/// Enterprise observability monitor foundation.
/// </summary>
public static class ForgeDatabaseObservability
{
    private static readonly ConcurrentQueue<ForgeDatabaseMetric> Metrics = new();

    public static void Record(string name, double value, string unit)
        => Metrics.Enqueue(new ForgeDatabaseMetric(name, value, unit, DateTimeOffset.UtcNow));

    public static IReadOnlyList<ForgeDatabaseMetric> Snapshot()
        => Metrics.ToArray();
}

/// <summary>
/// OpenTelemetry bridge foundation without forcing package dependency.
/// </summary>
public static class ForgeOpenTelemetryBridge
{
    public static readonly ActivitySource ActivitySource = new("ForgeORM");

    public static Activity? StartQueryActivity(string operation, string? tenantId = null)
    {
        var activity = ActivitySource.StartActivity(operation);
        activity?.SetTag("db.system", "forgeorm");
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            activity?.SetTag("forge.tenant_id", tenantId);
        }

        return activity;
    }
}

/// <summary>
/// Source-generator registration model foundation.
/// </summary>
public sealed record ForgeGeneratedArtifact(
    string Name,
    string Kind,
    string Path,
    string Description);

/// <summary>
/// Binary protocol optimization policy foundation.
/// </summary>
public sealed record ForgeBinaryOptimizationPolicy(
    bool UseBinaryImport,
    bool ReusePreparedStatements,
    bool UseStructuredParameters);

/// <summary>
/// AI-native deterministic fallback service.
/// </summary>
public static class ForgeAiNative
{
    public static string ToSql(string naturalLanguage)
    {
        if (naturalLanguage.Contains("top", StringComparison.OrdinalIgnoreCase) &&
            naturalLanguage.Contains("customers", StringComparison.OrdinalIgnoreCase))
        {
            return "SELECT TOP (10) CustomerId, SUM(GrandTotal) AS Revenue FROM Orders GROUP BY CustomerId ORDER BY Revenue DESC";
        }

        return "-- AI SQL generation extension point. Configure provider to generate SQL.";
    }

    public static IReadOnlyList<string> SuggestOptimizations(string sql)
        => ForgeQueryPlanAnalyzer.Analyze(sql).OptimizationHints;
}

/// <summary>
/// Advanced transaction policy foundation.
/// </summary>
public sealed record ForgeAdvancedTransactionPolicy(
    bool EnableRetry,
    bool EnableIdempotency,
    bool UseOutbox,
    int MaxRetries = 3);

/// <summary>
/// GraphQL bridge model foundation.
/// </summary>
public sealed record ForgeGraphQlProjection(
    string Entity,
    IReadOnlyList<string> Fields,
    string? Filter);

/// <summary>
/// Data virtualization registry foundation.
/// </summary>
public sealed class ForgeDataVirtualizationRegistry
{
    private readonly ConcurrentDictionary<string, ForgeVirtualDataSource> _sources = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ForgeVirtualDataSource source) => _sources[source.Name] = source;

    public IReadOnlyList<ForgeVirtualDataSource> Sources() => _sources.Values.ToList();
}

/// <summary>
/// Time-series query helper foundation.
/// </summary>
public static class ForgeTimeSeriesSql
{
    public static string BucketSql(string dateColumn, ForgeTimeBucket bucket)
    {
        return bucket.Unit.ToLowerInvariant() switch
        {
            "day" => $"CAST({dateColumn} AS date)",
            "month" => $"DATEFROMPARTS(YEAR({dateColumn}), MONTH({dateColumn}), 1)",
            "hour" => $"DATEADD(hour, DATEDIFF(hour, 0, {dateColumn}), 0)",
            _ => $"CAST({dateColumn} AS date)"
        };
    }
}

/// <summary>
/// Enterprise migration planner foundation.
/// </summary>
public static class ForgeEnterpriseMigrationPlanner
{
    public static IReadOnlyList<ForgeMigrationOperation> PlanAddColumn(
        string table,
        string column,
        string sqlType,
        bool nullable)
    {
        var nullSql = nullable ? "NULL" : "NOT NULL";
        return
        [
            new("AddColumn", $"{table}.{column}", $"ALTER TABLE {table} ADD {column} {sqlType} {nullSql};")
        ];
    }
}

/// <summary>
/// Enterprise admin dashboard snapshot.
/// </summary>
public sealed record ForgeAdminDashboardSnapshot(
    IReadOnlyList<ForgeEnterpriseFeatureResult> Features,
    IReadOnlyList<ForgeDatabaseMetric> Metrics,
    IReadOnlyList<ForgeMaterializedQuery> MaterializedQueries);
