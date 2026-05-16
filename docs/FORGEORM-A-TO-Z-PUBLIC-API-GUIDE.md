# ForgeORM A-to-Z Public API Guide

This guide is generated from the uploaded ForgeORM source and includes required graph APIs for parent-child insert, update, fetch, and delete.

## Required Graph APIs

### `InsertAsync<T>(T entity, CancellationToken cancellationToken = default)`

Inserts one entity. Use for a single row where no child collection should be automatically persisted.

**Parameters:** entity: object to insert. cancellationToken: cancels operation.

**Returns:** Task<int> affected rows.

```csharp
await db.InsertAsync(new Product { Name = "Mouse", Price = 4500 }, ct);
```

### `InsertManyAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)`

Inserts many independent entities in one transaction. Use when rows are not parent-child aggregates.

**Parameters:** entities: collection of rows. cancellationToken: cancels operation.

**Returns:** Task<int> inserted row count.

```csharp
await db.InsertManyAsync<Product>([
    new Product { Name = "Laptop", Price = 250000 },
    new Product { Name = "Mouse", Price = 4500 }
], ct);
```

### `InsertGraphAsync<T>(T entity, CancellationToken cancellationToken = default)`

Inserts a parent and all public child collection properties automatically. Parent is inserted first; generated parent key is copied into child foreign keys such as OrderId; child rows are inserted recursively in the same transaction.

**Parameters:** entity: aggregate root containing child collections. cancellationToken: cancels operation.

**Returns:** Task<T> same entity after generated keys and FK propagation.

```csharp
await db.InsertGraphAsync<Order>(
    new Order
    {
        CustomerId = customerId,
        OrderDate = DateTimeOffset.UtcNow,
        Items =
        [
            new OrderItem { ProductId = productId, Quantity = 2, UnitPrice = 5000 },
            new OrderItem { ProductId = anotherProductId, Quantity = 1, UnitPrice = 9000 }
        ],
        Payments =
        [
            new OrderPayment { Amount = 19000, PaymentMethod = "Card" }
        ]
    },
    ct);
```

### `UpdateGraphAsync<T>(T entity, bool deleteMissingChildren = false, CancellationToken cancellationToken = default)`

Updates parent and synchronizes child lists. Existing child rows with a key are updated; child rows without a key are inserted; when deleteMissingChildren is true, child rows missing from the submitted graph are removed.

**Parameters:** entity: aggregate root with desired graph state. deleteMissingChildren: enables full graph synchronization. cancellationToken: cancels operation.

**Returns:** Task<int> affected row count.

```csharp
order.Items[0].Quantity = 5;
order.Items.Add(new OrderItem { ProductId = newProductId, Quantity = 1, UnitPrice = 3000 });
await db.UpdateGraphAsync(order, deleteMissingChildren: true, ct);
```

### `GetGraphAsync<T>(object id, IEnumerable<string>? includes = null, CancellationToken cancellationToken = default)`

Fetches a parent row and loads selected child collections into it. If includes is null, all public collection properties are attempted.

**Parameters:** id: parent primary key. includes: collection property names, for example Items or Payments. cancellationToken: cancels operation.

**Returns:** Task<T?> parent with child collections or null.

```csharp
var order = await db.GetGraphAsync<Order>(
    id: orderId,
    includes: ["Items", "Payments", "Notes"],
    cancellationToken: ct);
```

### `DeleteGraphAsync<T>(object id, CancellationToken cancellationToken = default)`

Deletes child rows first and then deletes the parent row in one transaction. Use when the caller wants to delete the aggregate root with its children.

**Parameters:** id: parent primary key. cancellationToken: cancels operation.

**Returns:** Task<int> affected row count.

```csharp
await db.DeleteGraphAsync<Order>(orderId, ct);
```

## Public Method Inventory

### ForgeORM.AI.Advanced

- `public sealed record ForgeAiOptimizationRequest(string Sql, string Provider = "SqlServer", string? ExecutionPlan = null)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public sealed record ForgeAiOptimizationResult(IReadOnlyList<string> Issues, IReadOnlyList<string> Suggestions, IReadOnlyList<string> IndexRecommendations, string OptimizedSql)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public sealed record ForgeAiDiagnosticResult(string Severity, IReadOnlyList<string> Findings, IReadOnlyList<string> Fixes)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public sealed record ForgeGeneratedFile(string Path, string Content)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public sealed record ForgeMigrationPlan(string Name, IReadOnlyList<string> UpSql, IReadOnlyList<string> DownSql, IReadOnlyList<string> Warnings)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public ForgeAiOptimizationResult Optimize(ForgeAiOptimizationRequest request)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public ForgeAiDiagnosticResult Diagnose(string sql, TimeSpan elapsed, int rowCount)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public IReadOnlyList<ForgeGeneratedFile> GenerateMinimalApiCrud(string entityName, string routePrefix)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public ForgeMigrationPlan PlanAddColumn(string table, string column, string sqlType, bool nullable = true)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public Task<IReadOnlyList<ForgeVectorSearchResult>> SearchKnowledgeAsync(string text, CancellationToken cancellationToken = default)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

- `public static IServiceCollection AddForgeAdvancedAi(this IServiceCollection services)` (ForgeORM.AI.Advanced/ForgeAiAdvanced.cs)

### ForgeORM.AI.Agents

- `public sealed record ForgeAgentTask(string Goal, IReadOnlyDictionary<string,string>? Context = null)` (ForgeORM.AI.Agents/AIAgents.cs)

- `public sealed record ForgeAgentResult(string Agent, string Summary, IReadOnlyList<string> Actions, IReadOnlyList<string> Warnings)` (ForgeORM.AI.Agents/AIAgents.cs)

- `public Task<ForgeAgentResult> RunAsync(ForgeAgentTask task, CancellationToken cancellationToken = default)` (ForgeORM.AI.Agents/AIAgents.cs)

- `public sealed class ForgeAgentRunner(IEnumerable<IForgeAiAgent> agents)` (ForgeORM.AI.Agents/AIAgents.cs)

- `public async Task<IReadOnlyList<ForgeAgentResult>> RunAllAsync(ForgeAgentTask task, CancellationToken cancellationToken = default)` (ForgeORM.AI.Agents/AIAgents.cs)

- `public static IServiceCollection AddForgeAiAgents(this IServiceCollection services)` (ForgeORM.AI.Agents/AIAgents.cs)

### ForgeORM.AI.Memory

- `public sealed record ForgeMemoryEntry(string Scope, string Key, string Value, DateTimeOffset CreatedUtc, IReadOnlyDictionary<string,string>? Tags = null)` (ForgeORM.AI.Memory/AIMemory.cs)

- `public Task RememberAsync(ForgeMemoryEntry entry, CancellationToken cancellationToken = default)` (ForgeORM.AI.Memory/AIMemory.cs)

- `public Task<IReadOnlyList<ForgeMemoryEntry>> RecallAsync(string scope, string? query = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ForgeMemoryEntry>>(_entries.Where(x => x.Scope == scope && (string.IsNullOrWhiteSpace(query) || x.Key.Contains(query, StringComparison.OrdinalIg...` (ForgeORM.AI.Memory/AIMemory.cs)

- `public static IServiceCollection AddForgeAiMemory(this IServiceCollection services) => services.AddSingleton<IForgeAiMemoryStore, InMemoryForgeAiMemoryStore>()` (ForgeORM.AI.Memory/AIMemory.cs)

### ForgeORM.Abstractions

- `public static ForgeCommand Text(string sql, object? parameters = null, int? timeoutSeconds = null) => new()` (ForgeORM.Abstractions/Models.cs)

- `public static ForgeCommand StoredProcedure(string name, object? parameters = null, int? timeoutSeconds = null) => new()` (ForgeORM.Abstractions/Models.cs)

- `public string Parameter(string name)` (ForgeORM.Abstractions/Models.cs)

- `public sealed record ForgeFeatureDescriptor( string Code, string Name, ForgeReleasePhase Phase, ForgeFeatureStatus Status, string Description)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeModuleDescriptor( string Name, ForgeReleasePhase Phase, IReadOnlyList<ForgeFeatureDescriptor> Features)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeCompiledQueryKey( string Provider, string EntityName, string Shape, string? TenantId = null)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeTenantContext( string TenantId, string? ConnectionString = null, string? Schema = null)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeOutboxMessage( Guid Id, string EventType, string Payload, DateTimeOffset CreatedAt, string? TenantId = null, DateTimeOffset? ProcessedAt = null)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeReportColumn(string Name, string Expression, string? Alias = null)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeReportFilter(string Expression, object? Parameters = null)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeReportRequest( string Name, string From, IReadOnlyList<ForgeReportColumn> Columns, IReadOnlyList<ForgeReportFilter> Filters, string? GroupBy = null, string? OrderBy = null, int? Skip = null, int? Take = null)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeReportSql(string Sql, object? Parameters)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeAiQueryRequest( string Prompt, string Provider, string? EntityName = null, string? Schema = null, string? SafetyPolicy = null)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeAiQueryResult( string Sql, string Explanation, IReadOnlyList<string> Warnings, IReadOnlyList<string> SuggestedIndexes)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeScaffoldRequest(string ConnectionString, string Provider, string Namespace, string OutputPath)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeGeneratedFile(string Path, string Content)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeApiGenerationRequest(string EntityName, string RoutePrefix, string Namespace)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

- `public sealed record ForgeMigrationPlan(string Name, IReadOnlyList<string> UpSql, IReadOnlyList<string> DownSql)` (ForgeORM.Abstractions/V1V2V3/ForgePlatformContracts.cs)

### ForgeORM.Analytics

- `public ForgeQueryAnalysis Analyze(string sql)` (ForgeORM.Analytics/BasicForgeQueryAnalyzer.cs)

- `public ForgeDistributedFramePlan RepartitionBy(string column)` (ForgeORM.Analytics/ForgeAnalyticsAdvancedAdapters.cs)

- `public ForgeDistributedFramePlan Cache()` (ForgeORM.Analytics/ForgeAnalyticsAdvancedAdapters.cs)

- `public static ForgeDistributedFramePlan ToDistributedPlan(this ForgeDataFrame frame, string name = "local") => new()` (ForgeORM.Analytics/ForgeAnalyticsAdvancedAdapters.cs)

- `public static Task WriteParquetAsync(this ForgeDataFrame frame, string path, CancellationToken cancellationToken = default)` (ForgeORM.Analytics/ForgeAnalyticsAdvancedAdapters.cs)

- `public static ForgeDataFrame AiInsights(this ForgeDataFrame frame) => frame.Describe(frame.Columns.ToArray())` (ForgeORM.Analytics/ForgeAnalyticsAdvancedAdapters.cs)

- `public static ForgeDataFrame VectorizeText(this ForgeDataFrame frame, string textColumn, string vectorColumn)` (ForgeORM.Analytics/ForgeAnalyticsAdvancedAdapters.cs)

- `public ForgeAnalyticsQuery<T> From(string tableOrView)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeAnalyticsQuery<T> WhereSql(string sql)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeAnalyticsQuery<T> Select(Expression<Func<T, object?>> column, string? alias = null)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeAnalyticsQuery<T> SelectSql(string sql, string? alias = null)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeAnalyticsQuery<T> GroupBy(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeAnalyticsQuery<T> GroupBySql(params string[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeAnalyticsQuery<T> OrderBy(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeAnalyticsQuery<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeAnalyticsQuery<T> OrderBySql(params string[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> RowNumber() => new(this, "ROW_NUMBER()", null)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> Rank() => new(this, "RANK()", null)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> DenseRank() => new(this, "DENSE_RANK()", null)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> Ntile(int buckets)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> PercentRank() => new(this, "PERCENT_RANK()", null)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> CumeDist() => new(this, "CUME_DIST()", null)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> Count() => new(this, "COUNT(*)", null)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> Sum(Expression<Func<T, object?>> column)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> Avg(Expression<Func<T, object?>> column)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> Min(Expression<Func<T, object?>> column)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> Max(Expression<Func<T, object?>> column)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> Lag(Expression<Func<T, object?>> column, int offset = 1)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> Lead(Expression<Func<T, object?>> column, int offset = 1)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> FirstValue(Expression<Func<T, object?>> column)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> LastValue(Expression<Func<T, object?>> column)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> PercentileCont( Expression<Func<T, object?>> column, decimal percentile, string sqlType = "decimal(18,6)", string castType = "decimal(18,4)")` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> PercentileDisc( Expression<Func<T, object?>> column, decimal percentile, string sqlType = "decimal(18,6)", string castType = "decimal(18,4)")` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeRenderedAnalyticsSql Render()` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public async Task<IReadOnlyList<IDictionary<string, object?>>> ToDynamicListAsync( CancellationToken cancellationToken = default)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> PartitionBy(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> PartitionBySql(params string[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> OrderBy(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> OrderBySql(params string[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> OverAll()` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> RowsBetweenUnboundedPrecedingAndCurrentRow()` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> RowsBetweenUnboundedPrecedingAndUnboundedFollowing()` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> RowsBetweenPrecedingAndCurrentRow(int preceding)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowMetric<T> RowsBetweenCurrentRowAndFollowing(int following)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeAnalyticsQuery<T> As(string alias)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgePivotQuery<T> From(string tableOrView)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgePivotQuery<T> Rows(Expression<Func<T, object?>> row)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgePivotQuery<T> Columns(Expression<Func<T, object?>> column)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgePivotQuery<T> Values(Expression<Func<T, object?>> value)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgePivotQuery<T> Aggregate(ForgeSqlAggregate aggregate)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgePivotQuery<T> KnownColumns(params string[] columns)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeRenderedAnalyticsSql Render()` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public async Task<string> ToDynamicSqlServerPivotScriptAsync(CancellationToken cancellationToken = default)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public sealed record ForgeRenderedAnalyticsSql(string Sql)` (ForgeORM.Analytics/ForgeAnalyticsSqlBuilders.cs)

- `public ForgeWindowFunctionBuilder<T> PartitionBy(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Analytics/ForgeWindowFunctionBuilder.cs)

- `public ForgeWindowFunctionBuilder<T> OrderBy(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Analytics/ForgeWindowFunctionBuilder.cs)

- `public ForgeWindowFunctionBuilder<T> OrderByDescending(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Analytics/ForgeWindowFunctionBuilder.cs)

- `public ForgeWindowFunctionBuilder<T> RowsBetweenUnboundedPrecedingAndCurrentRow()` (ForgeORM.Analytics/ForgeWindowFunctionBuilder.cs)

- `public ForgeWindowFunctionBuilder<T> RowsBetweenUnboundedPrecedingAndUnboundedFollowing()` (ForgeORM.Analytics/ForgeWindowFunctionBuilder.cs)

- `public ForgeWindowFunctionBuilder<T> RowsBetweenPrecedingAndCurrentRow(int preceding)` (ForgeORM.Analytics/ForgeWindowFunctionBuilder.cs)

- `public ForgeWindowFunctionBuilder<T> RowsBetweenCurrentRowAndFollowing(int following)` (ForgeORM.Analytics/ForgeWindowFunctionBuilder.cs)

- `public ForgeAnalyticsQuery<T> As(string alias) => _metric.As(alias)` (ForgeORM.Analytics/ForgeWindowFunctionBuilder.cs)

- `public sealed record ForgeEnterpriseAnalysis( IReadOnlyList<string> Warnings, IReadOnlyList<string> SuggestedIndexes, IReadOnlyList<string> OptimizationHints)` (ForgeORM.Analytics/V1V2V3/ForgeEnterpriseAnalyzer.cs)

- `public ForgeEnterpriseAnalysis AnalyzeSql(string sql)` (ForgeORM.Analytics/V1V2V3/ForgeEnterpriseAnalyzer.cs)

### ForgeORM.AspNetCore

- `public void UseSqlServer(string connectionString)` (ForgeORM.AspNetCore/ForgeOrmServiceCollectionExtensions.cs)

- `public void UsePostgreSql(string connectionString)` (ForgeORM.AspNetCore/ForgeOrmServiceCollectionExtensions.cs)

- `public void UseMySql(string connectionString)` (ForgeORM.AspNetCore/ForgeOrmServiceCollectionExtensions.cs)

- `public void UseOracle(string connectionString)` (ForgeORM.AspNetCore/ForgeOrmServiceCollectionExtensions.cs)

- `public void UseSqlite(string connectionString)` (ForgeORM.AspNetCore/ForgeOrmServiceCollectionExtensions.cs)

- `public static IServiceCollection AddForgeOrm(this IServiceCollection services, Action<ForgeOrmOptions> configure)` (ForgeORM.AspNetCore/ForgeOrmServiceCollectionExtensions.cs)

### ForgeORM.Caching.Redis

- `public sealed record ForgeCacheOptions(string KeyPrefix = "forgeorm", TimeSpan DefaultTtl = default)` (ForgeORM.Caching.Redis/ForgeRedisCaching.cs)

- `public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => _cache.RemoveAsync(BuildKey(key), cancellationToken)` (ForgeORM.Caching.Redis/ForgeRedisCaching.cs)

- `public Task RemoveAsync(string key, CancellationToken cancellationToken = default)` (ForgeORM.Caching.Redis/ForgeRedisCaching.cs)

- `public static IServiceCollection AddForgeRedisQueryCaching(this IServiceCollection services, ForgeCacheOptions? options = null)` (ForgeORM.Caching.Redis/ForgeRedisCaching.cs)

- `public static IServiceCollection AddForgeMemoryQueryCaching(this IServiceCollection services)` (ForgeORM.Caching.Redis/ForgeRedisCaching.cs)

### ForgeORM.Cloud

- `public sealed record CloudDeploymentRequest(string AppName, string Provider, string ContainerImage, int Replicas = 2, string? Region = null)` (ForgeORM.Cloud/Cloud.cs)

- `public sealed record CloudDeploymentArtifacts(string Dockerfile, string KubernetesYaml, string Terraform, string HelmValues)` (ForgeORM.Cloud/Cloud.cs)

- `public CloudDeploymentArtifacts Generate(CloudDeploymentRequest r)` (ForgeORM.Cloud/Cloud.cs)

- `public static IServiceCollection AddForgeCloudDeployment(this IServiceCollection services) => services.AddSingleton<IForgeDeploymentGenerator, ForgeDeploymentGenerator>()` (ForgeORM.Cloud/Cloud.cs)

### ForgeORM.Core

- `public static int Execute(DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null) => ExecuteAsync(connection, sql, parameters, transaction, commandType, timeoutSeconds).GetAwaiter().GetResult()` (ForgeORM.Core/ForgeAdo.cs)

- `public static async Task<int> ExecuteAsync( DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeAdo.cs)

- `public static DbCommand CreateCommand( DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)` (ForgeORM.Core/ForgeAdo.cs)

- `public static async Task<IReadOnlyList<IDictionary<string, object?>>> QueryDynamicAsync( DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeAdo.cs)

- `public void BulkDelete(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id") => BulkDeleteAsync(tableName, ids, keyColumn).GetAwaiter().GetResult()` (ForgeORM.Core/ForgeDb.RepositoryBulkTransaction.cs)

- `public Task BulkDeleteAsync(string tableName, IReadOnlyCollection<int> ids, string keyColumn = "Id", CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeDb.RepositoryBulkTransaction.cs)

- `public IForgeTransaction BeginTransaction()` (ForgeORM.Core/ForgeDb.RepositoryBulkTransaction.cs)

- `public async Task<IForgeTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeDb.RepositoryBulkTransaction.cs)

- `public ForgeQueryAnalysis Analyze(string sql) => _analyzer.Analyze(sql)` (ForgeORM.Core/ForgeDb.RepositoryBulkTransaction.cs)

- `public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null)` (ForgeORM.Core/ForgeDb.cs)

- `public async Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeDb.cs)

- `public IForgeGridReader QueryMultiple(string sql, object? parameters = null, int? timeoutSeconds = null)` (ForgeORM.Core/ForgeDb.cs)

- `public async Task<IForgeGridReader> QueryMultipleAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeDb.cs)

- `public int ExecuteProcedure(string procedureName, object? parameters = null, int? timeoutSeconds = null)` (ForgeORM.Core/ForgeDb.cs)

- `public async Task<int> ExecuteProcedureAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeDb.cs)

- `public IForgeGridReader QueryProcedureMultiple(string procedureName, object? parameters = null, int? timeoutSeconds = null)` (ForgeORM.Core/ForgeDb.cs)

- `public async Task<IForgeGridReader> QueryProcedureMultipleAsync(string procedureName, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeDb.cs)

- `public async Task<IReadOnlyList<IDictionary<string, object?>>> QueryDynamicAsync( string sql, object? parameters = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeDb.cs)

- `public static int Execute(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null)` (ForgeORM.Core/ForgeDbConnectionExtensions.cs)

- `public static async Task<int> ExecuteAsync(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, CommandType commandType = CommandType.Text, int? timeoutSeconds = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeDbConnectionExtensions.cs)

- `public static IForgeGridReader QueryMultiple(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, int? timeoutSeconds = null)` (ForgeORM.Core/ForgeDbConnectionExtensions.cs)

- `public static async Task<IForgeGridReader> QueryMultipleAsync(this DbConnection connection, string sql, object? parameters = null, DbTransaction? transaction = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/ForgeDbConnectionExtensions.cs)

- `public static object? Map(Type type, DbDataReader reader)` (ForgeORM.Core/ForgeMaterializer.cs)

- `public static bool IsSafeIdentifier(string name) => !string.IsNullOrWhiteSpace(name) && SafeName.IsMatch(name)` (ForgeORM.Core/ForgeSqlNameValidator.cs)

- `public static string EscapeIdentifier(string name)` (ForgeORM.Core/ForgeSqlNameValidator.cs)

- `public static object? ToDatabase(object? value, Type? declaredType = null)` (ForgeORM.Core/ForgeValueConverter.cs)

- `public static object? FromDatabase(object? value, Type targetType)` (ForgeORM.Core/ForgeValueConverter.cs)

- `public ForgeGraphParentOptions<TParent> Parent()` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public ForgeGraphChildOptions<TDto, TChildEntity, TChildDto> UseSqlServerTvp( string tableType, string procedure, string parameterName = "@Items")` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public async Task InsertAsync(DbConnection connection, DbTransaction transaction, TDto dto, object parentKey, CancellationToken cancellationToken)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static Type StorageType(PropertyInfo property)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static object? ToDatabaseValue(object? value, PropertyInfo? property = null)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static object? ToEnumOrValue(object? value, Type targetType)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static void ConfigureStructured(DbParameter parameter, string tableType)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static void Copy(object source, object target)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static object? ConvertTo(object? value, Type targetType)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static ForgeEntityShape For(Type type)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static string ResolveTableName(Type type)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static string ColumnName(PropertyInfo property) => property.GetCustomAttribute<ForgeColumnAttribute>()` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static bool IsComputed(PropertyInfo property) => property.GetCustomAttribute<ForgeComputedAttribute>()` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static void EnsureGeneratedKey(object entity)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public static PropertyInfo Property(Expression expression)` (ForgeORM.Core/GraphInsert/ForgeGraphInsert.cs)

- `public ForgeSearch<T> Select(params string[] columns)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> Select(params Expression<Func<T, object?>>[] columns)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> From(string table)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> FromSql(string sql)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> Where(Expression<Func<T, bool>> predicate)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate) => condition ? Where(predicate)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> Where(string sql, object? parameters = null)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> WhereIf(bool condition, string sql, object? parameters = null) => condition ? Where(sql, parameters)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> OptionalLike(Expression<Func<T, string?>> column, string? value) => OptionalLike(ForgeSearchExpression.MemberName(column), value)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> OptionalLike(string column, string? value)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> OrderBy(Expression<Func<T, object?>> column)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> OrderByDescending(Expression<Func<T, object?>> column)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> OrderBy(string orderBy)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeSearch<T> Page(int page, int pageSize)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeRenderedSearchSql Render()` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public async Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public async Task<ForgePagedResult<T>> ToPagedAsync(CancellationToken cancellationToken = default)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeProcedureSearch<T> With(string name, object? value)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeProcedureSearch<T> WithOptional(string name, object? value)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public ForgeProcedureSearch<T> Page(int page, int pageSize)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default) => _db.QueryProcedureAsync<T>(_procedureName, _parameters, cancellationToken: cancellationToken)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public async Task<ForgePagedResult<T>> ToPagedAsync(CancellationToken cancellationToken = default)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public sealed record ForgeRenderedSearchSql(string Sql, object? Parameters = null)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public static string MemberName(Expression expression)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public static string ResolveTableName(Type type)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public static object? NormalizeParameterValue(object? value, MemberInfo? member = null)` (ForgeORM.Core/Search/ForgeSearch.cs)

- `public void Dispose()` (ForgeORM.Core/Support.cs)

- `public ForgeEntityMetadata Resolve(Type type)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> Where(Expression<Func<T, bool>> predicate)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> Where(string condition, object? parameters = null) => WhereSql(condition, parameters)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> WhereSql(string condition, object? parameters = null)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate) => condition ? Where(predicate)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null) => condition ? WhereSql(sqlCondition, parameters)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> OrderBy(Expression<Func<T, object>> keySelector)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> OrderBy(string orderBy) => OrderBySql(orderBy)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> OrderBySql(string orderBy)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> Skip(int count)` (ForgeORM.Core/Support.cs)

- `public IForgeQuery<T> Take(int count)` (ForgeORM.Core/Support.cs)

- `public bool Any() => _db.ExecuteScalar<int>(BuildAnySql(), BuildParameters())` (ForgeORM.Core/Support.cs)

- `public async Task<bool> AnyAsync(CancellationToken cancellationToken = default) => await _db.ExecuteScalarAsync<int>(BuildAnySql(), BuildParameters(), cancellationToken: cancellationToken)` (ForgeORM.Core/Support.cs)

- `public IReadOnlyList<T> ToList() => _db.Query<T>(BuildSql(), BuildParameters()).ToList()` (ForgeORM.Core/Support.cs)

- `public Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default) => _db.QueryAsync<T>(BuildSql(), BuildParameters(), cancellationToken: cancellationToken)` (ForgeORM.Core/Support.cs)

- `public T? FirstOrDefault()` (ForgeORM.Core/Support.cs)

- `public Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)` (ForgeORM.Core/Support.cs)

- `public int Count() => _db.ExecuteScalar<int>(BuildCountSql(), BuildParameters())` (ForgeORM.Core/Support.cs)

- `public async Task<int> CountAsync(CancellationToken cancellationToken = default) => await _db.ExecuteScalarAsync<int>(BuildCountSql(), BuildParameters(), cancellationToken: cancellationToken)` (ForgeORM.Core/Support.cs)

- `public bool Any(string parentSql, object? parameters = null)` (ForgeORM.Core/Support.cs)

- `public async Task<bool> AnyAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/Support.cs)

- `public TParent? FirstOrDefault(string parentSql, object? parameters = null) => FirstOrDefaultAsync(parentSql, parameters).GetAwaiter().GetResult()` (ForgeORM.Core/Support.cs)

- `public async Task<TParent?> FirstOrDefaultAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default) => (await ToListAsync(parentSql, parameters, cancellationToken)).FirstOrDefault()` (ForgeORM.Core/Support.cs)

- `public IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null) => ToListAsync(parentSql, parameters).GetAwaiter().GetResult()` (ForgeORM.Core/Support.cs)

- `public async Task<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)` (ForgeORM.Core/Support.cs)

- `public static ForgeTransaction Begin(DbConnection connection) => new(connection, connection.BeginTransaction())` (ForgeORM.Core/Support.cs)

- `public static async Task<ForgeTransaction> BeginAsync(DbConnection connection, CancellationToken ct) => new(connection, await connection.BeginTransactionAsync(ct))` (ForgeORM.Core/Support.cs)

- `public int Execute(string sql, object? parameters = null, int? timeoutSeconds = null) => ForgeAdo.Execute(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds)` (ForgeORM.Core/Support.cs)

- `public Task<int> ExecuteAsync(string sql, object? parameters = null, int? timeoutSeconds = null, CancellationToken cancellationToken = default) => ForgeAdo.ExecuteAsync(_connection, sql, parameters, _transaction, timeoutSeconds: timeoutSeconds, cancellationToken: cancellationToken)` (ForgeORM.Core/Support.cs)

- `public void Commit() => _transaction.Commit()` (ForgeORM.Core/Support.cs)

- `public Task CommitAsync(CancellationToken cancellationToken = default) => _transaction.CommitAsync(cancellationToken)` (ForgeORM.Core/Support.cs)

- `public void Rollback() => _transaction.Rollback()` (ForgeORM.Core/Support.cs)

- `public Task RollbackAsync(CancellationToken cancellationToken = default) => _transaction.RollbackAsync(cancellationToken)` (ForgeORM.Core/Support.cs)

- `public void Dispose()` (ForgeORM.Core/Support.cs)

- `public async ValueTask DisposeAsync()` (ForgeORM.Core/Support.cs)

- `public bool TryGet(ForgeCompiledQueryKey key, out string sql) => _cache.TryGetValue(key, out sql!)` (ForgeORM.Core/V1V2V3/ForgePlatform.cs)

- `public void Set(ForgeCompiledQueryKey key, string sql)` (ForgeORM.Core/V1V2V3/ForgePlatform.cs)

- `public void Clear() => _cache.Clear()` (ForgeORM.Core/V1V2V3/ForgePlatform.cs)

- `public Task RemoveAsync(string key, CancellationToken cancellationToken = default)` (ForgeORM.Core/V1V2V3/ForgePlatform.cs)

- `public Task EnqueueAsync(ForgeOutboxMessage message, CancellationToken cancellationToken = default)` (ForgeORM.Core/V1V2V3/ForgePlatform.cs)

- `public Task<IReadOnlyList<ForgeOutboxMessage>> GetPendingAsync(int take = 100, CancellationToken cancellationToken = default)` (ForgeORM.Core/V1V2V3/ForgePlatform.cs)

- `public Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)` (ForgeORM.Core/V1V2V3/ForgePlatform.cs)

- `public ForgeReportSql Build(ForgeReportRequest request, string provider = "SqlServer")` (ForgeORM.Core/V1V2V3/ForgePlatform.cs)

### ForgeORM.DataFrame

- `public abstract object? Compute(IEnumerable<object?> values)` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public static ForgeAgg Count() => new CountAgg()` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public static ForgeAgg Sum() => new SumAgg()` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public static ForgeAgg Avg() => new AvgAgg()` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public static ForgeAgg Min() => new MinAgg()` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public static ForgeAgg Max() => new MaxAgg()` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public static ForgeAgg Median() => new PercentileAgg(0.5m, "Median")` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public static ForgeAgg Percentile(decimal percentile) => new PercentileAgg(percentile, "P" + (int)(percentile * 100))` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public override object Compute(IEnumerable<object?> values) => values.Count(v => v is not null and not DBNull)` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public override object Compute(IEnumerable<object?> values) => values.Select(ForgeDataFrame.ToDecimal).Where(x => x.HasValue).Sum(x => x!.Value)` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public override object? Compute(IEnumerable<object?> values)` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public override object? Compute(IEnumerable<object?> values)` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public override object? Compute(IEnumerable<object?> values)` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public override object? Compute(IEnumerable<object?> values)` (ForgeORM.DataFrame/ForgeAgg.cs)

- `public static async Task<ForgeDataFrame> FromCsvAsync( Stream stream, CancellationToken cancellationToken = default)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static async Task<ForgeDataFrame> FromJsonAsync( Stream stream, CancellationToken cancellationToken = default)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static ForgeDataFrame FromCsv(string path, bool hasHeader = true, char delimiter = ',') => FromCsvText(File.ReadAllText(path), hasHeader, delimiter)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static async Task<ForgeDataFrame> FromCsvAsync(string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default) => FromCsvText(await File.ReadAllTextAsync(path, cancellationToken), hasHeader, delimiter)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static async Task<ForgeDataFrame> FromCsvAsync(Stream stream, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static ForgeDataFrame FromCsvText(string csv, bool hasHeader = true, char delimiter = ',')` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static ForgeDataFrame FromJson(string path) => FromJsonText(File.ReadAllText(path))` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static async Task<ForgeDataFrame> FromJsonAsync(string path, CancellationToken cancellationToken = default) => FromJsonText(await File.ReadAllTextAsync(path, cancellationToken))` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static async Task<ForgeDataFrame> FromJsonv1Async(Stream stream, CancellationToken cancellationToken = default)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static ForgeDataFrame FromJsonText(string json)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static async Task<ForgeDataFrame> FromCsvAsync( string path, CancellationToken cancellationToken = default)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public int ToTable(ForgeDb db, string tableName, bool createIfNotExists = true, bool dropIfExists = false)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public async Task<int> ToTableAsync( ForgeDb db, string tableName, bool createIfNotExists = true, bool dropIfExists = false, CancellationToken cancellationToken = default)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public IReadOnlyList<IDictionary<string, object?>> ToDictionaries() => _rows.Select(r => new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase)).Cast<IDictionary<string, object?>>().ToList()` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Head(int count = 5) => new(_rows.Take(Math.Max(count, 0)))` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Tail(int count = 5) => new(_rows.Skip(Math.Max(0, _rows.Count - Math.Max(count, 0))))` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Select(params string[] columns) => new(_rows.Select(row => columns.ToDictionary(c => c, c => row.TryGetValue(c, out var v) ? v : null, StringComparer.OrdinalIgnoreCase)))` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Rename(string oldName, string newName)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Where(Func<IReadOnlyDictionary<string, object?>, bool> predicate) => new(_rows.Where(r => predicate(r)))` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame SortBy(string column, bool descending = false)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Assign(string column, Func<IReadOnlyDictionary<string, object?>, object?> valueFactory)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame FillNa(object? value, params string[] columns)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame DropNa(params string[] columns)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame DropDuplicates(params string[] columns)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeGroupBy GroupBy(params string[] columns) => new(this, columns)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame PivotTable(string rows, string columns, string values, ForgeAgg aggregate)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Melt(string[] idVars, string[] valueVars, string variableName = "variable", string valueName = "value")` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Merge(ForgeDataFrame right, string leftOn, string rightOn, ForgeJoinKind join = ForgeJoinKind.Inner)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Rolling(string valueColumn, int window, string outputColumn, ForgeAgg aggregate)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public ForgeDataFrame Describe(params string[] numericColumns)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public Microsoft.Data.Analysis.DataFrame ToMicrosoftDataFrame()` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static object? Get(IReadOnlyDictionary<string, object?> row, string column) => row.TryGetValue(column, out var value)` (ForgeORM.DataFrame/ForgeDataFrame.cs)

- `public static ForgeDataFrame ReadCsv(string path, bool hasHeader = true, char delimiter = ',') => ForgeDataFrame.FromCsv(path, hasHeader, delimiter)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public static Task<ForgeDataFrame> ReadCsvAsync(string path, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default) => ForgeDataFrame.FromCsvAsync(path, hasHeader, delimiter, cancellationToken)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public static Task<ForgeDataFrame> ReadCsvAsync(Stream stream, bool hasHeader = true, char delimiter = ',', CancellationToken cancellationToken = default) => ForgeDataFrame.FromCsvAsync(stream, hasHeader, delimiter, cancellationToken)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public static ForgeDataFrame ReadJson(string path) => ForgeDataFrame.FromJson(path)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public static Task<ForgeDataFrame> ReadJsonAsync(string path, CancellationToken cancellationToken = default) => ForgeDataFrame.FromJsonAsync(path, cancellationToken)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public static Task<ForgeDataFrame> ReadJsonAsync(Stream stream, CancellationToken cancellationToken = default) => ForgeDataFrame.FromJsonAsync(stream, cancellationToken)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public ForgeFrameQuery<T> From(string table)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public ForgeFrameQuery<T> FromSql(string sql, object? parameters = null)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public ForgeFrameQuery<T> WhereSql(string condition)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public ForgeFrameQuery<T> OrderBy(string orderBy)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public async Task<ForgeDataFrame> ToFrameAsync(CancellationToken cancellationToken = default)` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public ForgeDataFrame ToFrame()` (ForgeORM.DataFrame/ForgeFrameExtensions.cs)

- `public ForgeDataFrame Agg(params ForgeAggregation[] aggregations)` (ForgeORM.DataFrame/ForgeGroupBy.cs)

- `public sealed record ForgeAggregation(string Column, ForgeAgg Aggregate, string? Alias = null)` (ForgeORM.DataFrame/ForgeGroupBy.cs)

- `public static ForgeAggregation Count(string column = "*", string? alias = null) => new(column, ForgeAgg.Count(), alias ?? "Count")` (ForgeORM.DataFrame/ForgeGroupBy.cs)

- `public static ForgeAggregation Sum(string column, string? alias = null) => new(column, ForgeAgg.Sum(), alias)` (ForgeORM.DataFrame/ForgeGroupBy.cs)

- `public static ForgeAggregation Avg(string column, string? alias = null) => new(column, ForgeAgg.Avg(), alias)` (ForgeORM.DataFrame/ForgeGroupBy.cs)

- `public static ForgeAggregation Min(string column, string? alias = null) => new(column, ForgeAgg.Min(), alias)` (ForgeORM.DataFrame/ForgeGroupBy.cs)

- `public static ForgeAggregation Max(string column, string? alias = null) => new(column, ForgeAgg.Max(), alias)` (ForgeORM.DataFrame/ForgeGroupBy.cs)

- `public static ForgeAggregation Median(string column, string? alias = null) => new(column, ForgeAgg.Median(), alias)` (ForgeORM.DataFrame/ForgeGroupBy.cs)

- `public static ForgeAggregation Percentile(string column, decimal percentile, string? alias = null) => new(column, ForgeAgg.Percentile(percentile), alias)` (ForgeORM.DataFrame/ForgeGroupBy.cs)

### ForgeORM.DataVirtualization

- `public sealed record VirtualDataSource(string Name, string Kind, string ConnectionName)` (ForgeORM.DataVirtualization/DataVirtualization.cs)

- `public sealed record FederatedQuery(string Name, string Query, IReadOnlyList<string> Sources)` (ForgeORM.DataVirtualization/DataVirtualization.cs)

- `public sealed record FederatedQueryPlan(IReadOnlyList<string> SourcePlans, string MergeStrategy, string SqlPreview)` (ForgeORM.DataVirtualization/DataVirtualization.cs)

- `public FederatedQueryPlan Plan(FederatedQuery query, IReadOnlyList<VirtualDataSource> sources)` (ForgeORM.DataVirtualization/DataVirtualization.cs)

- `public FederatedPlanResult Plan(string query, IReadOnlyList<FederatedDataSource> sources)` (ForgeORM.DataVirtualization/DataVirtualization.cs)

- `public FederatedPlanResult Plan(FederatedPlanRequest request)` (ForgeORM.DataVirtualization/DataVirtualization.cs)

- `public static IServiceCollection AddForgeDataVirtualization(this IServiceCollection services) => services.AddSingleton<IForgeFederatedQueryPlanner, ForgeFederatedQueryPlanner>()` (ForgeORM.DataVirtualization/DataVirtualization.cs)

### ForgeORM.EventSourcing

- `public sealed record ForgeStoredEvent(long Sequence, string StreamId, string EventType, string PayloadJson, DateTimeOffset OccurredUtc)` (ForgeORM.EventSourcing/EventSourcing.cs)

- `public sealed record ForgeSnapshot(string StreamId, long Version, string PayloadJson, DateTimeOffset CreatedUtc)` (ForgeORM.EventSourcing/EventSourcing.cs)

- `public Task AppendAsync(string streamId, IEnumerable<IForgeEvent> events, CancellationToken cancellationToken = default)` (ForgeORM.EventSourcing/EventSourcing.cs)

- `public Task<IReadOnlyList<ForgeStoredEvent>> ReadStreamAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ForgeStoredEvent>>(_events.Where(x => x.StreamId == streamId && x.Sequence >= fromVersion).OrderBy(x => x.Sequence).ToList())` (ForgeORM.EventSourcing/EventSourcing.cs)

- `public Task<IReadOnlyList<ForgeStoredEvent>> ReadAllAsync(long fromSequence = 0, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ForgeStoredEvent>>(_events.Where(x => x.Sequence >= fromSequence).OrderBy(x => x.Sequence).ToList())` (ForgeORM.EventSourcing/EventSourcing.cs)

- `public static IServiceCollection AddForgeEventSourcing(this IServiceCollection services) => services.AddSingleton<IForgeEventStore, InMemoryForgeEventStore>()` (ForgeORM.EventSourcing/EventSourcing.cs)

### ForgeORM.Identity

- `public sealed record ForgePrincipal(string UserId, IReadOnlyList<string> Roles, IReadOnlyDictionary<string,string> Claims)` (ForgeORM.Identity/Identity.cs)

- `public sealed record ForgePolicyRequirement(string Resource, string Action, string? TenantId = null)` (ForgeORM.Identity/Identity.cs)

- `public sealed record ForgePolicyDecision(bool Allowed, string Reason)` (ForgeORM.Identity/Identity.cs)

- `public ForgePolicyDecision Authorize(ForgePrincipal principal, ForgePolicyRequirement requirement)` (ForgeORM.Identity/Identity.cs)

- `public static IServiceCollection AddForgeIdentityPolicies(this IServiceCollection services) => services.AddSingleton<IForgePolicyEngine, ForgePolicyEngine>()` (ForgeORM.Identity/Identity.cs)

- `public ForgePrincipal ToPrincipal() => new(UserId, Roles, Claims)` (ForgeORM.Identity/Identity.cs)

- `public ForgePolicyRequirement ToRequirement() => new(Resource, Action, TenantId)` (ForgeORM.Identity/Identity.cs)

### ForgeORM.Intelligence

- `public ForgeSqlSuggestionResult Suggest(string partialSql, ForgeSqlContext context)` (ForgeORM.Intelligence/ForgeSqlIntelligence.cs)

- `public ForgeSqlCorrectionResult Correct(string sql, ForgeSqlContext context)` (ForgeORM.Intelligence/ForgeSqlIntelligence.cs)

- `public ForgeSqlCompletionResult Complete(string partialSql, int cursorPosition, ForgeSqlContext context)` (ForgeORM.Intelligence/ForgeSqlIntelligence.cs)

- `public Task<ForgeAiQueryResult> GenerateSqlAsync(ForgeAiQueryRequest request, CancellationToken cancellationToken = default)` (ForgeORM.Intelligence/V1V2V3/ForgeAiAssistant.cs)

- `public IReadOnlyList<ForgeGeneratedFile> GenerateCrudApi(ForgeApiGenerationRequest request)` (ForgeORM.Intelligence/V1V2V3/ForgeAiAssistant.cs)

- `public ForgeMigrationPlan Plan(string name, IReadOnlyList<string> currentSchema, IReadOnlyList<string> targetSchema)` (ForgeORM.Intelligence/V1V2V3/ForgeAiAssistant.cs)

- `public Task<IReadOnlyList<ForgeGeneratedFile>> ScaffoldAsync(ForgeScaffoldRequest request, CancellationToken cancellationToken = default)` (ForgeORM.Intelligence/V1V2V3/ForgeAiAssistant.cs)

### ForgeORM.LowCode

- `public sealed record LowCodeField(string Name, string Type, bool Required = false, string? DisplayName = null)` (ForgeORM.LowCode/LowCode.cs)

- `public sealed record LowCodeEntity(string Name, IReadOnlyList<LowCodeField> Fields)` (ForgeORM.LowCode/LowCode.cs)

- `public sealed record LowCodeApp(string Name, IReadOnlyList<LowCodeEntity> Entities, IReadOnlyList<LowCodePage> Pages)` (ForgeORM.LowCode/LowCode.cs)

- `public sealed record LowCodePage(string Name, string Route, string Entity, string Kind)` (ForgeORM.LowCode/LowCode.cs)

- `public sealed record GeneratedEnterpriseApp(string Name, IReadOnlyList<LowCodeEntity> Entities, IReadOnlyList<string> Modules, IReadOnlyList<string> ApiRoutes)` (ForgeORM.LowCode/LowCode.cs)

- `public GeneratedEnterpriseApp GenerateErp(string businessDomain, IReadOnlyList<string> modules)` (ForgeORM.LowCode/LowCode.cs)

- `public string GenerateMinimalApi(LowCodeEntity entity)` (ForgeORM.LowCode/LowCode.cs)

- `public string GenerateReactForm(LowCodeEntity entity)` (ForgeORM.LowCode/LowCode.cs)

- `public static IServiceCollection AddForgeLowCode(this IServiceCollection services) => services.AddSingleton<IForgeLowCodeEngine, ForgeLowCodeEngine>()` (ForgeORM.LowCode/LowCode.cs)

### ForgeORM.Marketplace

- `public sealed record ForgeMarketplaceItem(string Id, string Name, string Category, string Version, string Author, string Description, IReadOnlyList<string> Tags)` (ForgeORM.Marketplace/Marketplace.cs)

- `public IReadOnlyList<ForgeMarketplaceItem> Search(string? query = null, string? category = null) => _items.Where(x => (string.IsNullOrWhiteSpace(query) || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(cat...` (ForgeORM.Marketplace/Marketplace.cs)

- `public void Publish(ForgeMarketplaceItem item) => _items.Add(item)` (ForgeORM.Marketplace/Marketplace.cs)

- `public static IServiceCollection AddForgeMarketplace(this IServiceCollection services) => services.AddSingleton<IForgeMarketplaceCatalog, InMemoryForgeMarketplaceCatalog>()` (ForgeORM.Marketplace/Marketplace.cs)

### ForgeORM.NextGen

- `public void AppendLiteral(string value) => _sql.Append(value)` (ForgeORM.NextGen/ForgeIdeIntegration.cs)

- `public ForgeSchemaAwareSql ToSql()` (ForgeORM.NextGen/ForgeIdeIntegration.cs)

- `public ForgeTraceLink CreateTrace(string sql, object? parameters, string providerName)` (ForgeORM.NextGen/ForgeIdeIntegration.cs)

- `public static string ExecuteTransparent(this IForgeDb db, string sql, object? parameters = null)` (ForgeORM.NextGen/ForgeNextGenExtensions.cs)

- `public IForgeSmartQuery<T> WhereSql(FormattableString sql)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public IForgeSmartQuery<T> WithPolicy(ForgeResiliencePolicy policy)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public IForgeSmartQuery<T> AsCached(TimeSpan duration, string? key = null)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public IForgeSmartQuery<T> Mock(IEnumerable<T> rows)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public IForgeSmartQuery<T> IncludeGraph(int maxDepth = 2)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public IForgeSmartQuery<T> ShadowProperty(string name)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public ForgeTransparentCommand ExecuteTransparent()` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public ForgeExplainResult Explain()` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public Task<ForgeExplainResult> ExplainAsync(CancellationToken cancellationToken = default)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public JsonDocument IntoJsonDocument()` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public async Task<JsonDocument> IntoJsonDocumentAsync(CancellationToken cancellationToken = default)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public string IntoJson()` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public async Task<string> IntoJsonAsync(CancellationToken cancellationToken = default)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public async IAsyncEnumerable<T> StreamAllAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public IReadOnlyList<T> ToList()` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public async Task<IReadOnlyList<T>> ToListAsync(CancellationToken cancellationToken = default)` (ForgeORM.NextGen/ForgeSmartQuery.cs)

- `public int Execute(IForgeDb db) => db.Execute(Sql, Parameters)` (ForgeORM.NextGen/NextGenModels.cs)

- `public Task<int> ExecuteAsync(IForgeDb db, CancellationToken cancellationToken = default) => db.ExecuteAsync(Sql, Parameters, cancellationToken: cancellationToken)` (ForgeORM.NextGen/NextGenModels.cs)

- `public object? ShadowProperty(string name) => ShadowValues.TryGetValue(name, out var value)` (ForgeORM.NextGen/NextGenModels.cs)

- `public static ForgeSafeSql From(FormattableString formattable)` (ForgeORM.NextGen/NextGenModels.cs)

### ForgeORM.Observability.AI

- `public sealed record ForgeObservabilityInsight(string Severity, string Title, string Recommendation)` (ForgeORM.Observability.AI/ObservabilityAI.cs)

- `public IReadOnlyList<ForgeObservabilityInsight> Analyze(ForgeMonitoringSnapshot snapshot)` (ForgeORM.Observability.AI/ObservabilityAI.cs)

- `public static IServiceCollection AddForgeAiObservability(this IServiceCollection services) => services.AddSingleton<IForgeAiObservabilityAnalyzer, ForgeAiObservabilityAnalyzer>()` (ForgeORM.Observability.AI/ObservabilityAI.cs)

### ForgeORM.Providers.MySql

- `public DbConnection CreateConnection(string connectionString) => new MySqlConnection(connectionString)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildInsertSql(e), entity)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildUpdateSql(e), entity)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildPage(ForgePageRequest r)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildCount(string baseSql, object? parameters = null) => ForgeCommand.Text($"SELECT COUNT(1)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

- `public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null)` (ForgeORM.Providers.MySql/MySqlForgeProvider.cs)

### ForgeORM.Providers.Oracle

- `public DbConnection CreateConnection(string connectionString) => new OracleConnection(connectionString)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildInsertSql(e), entity)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildUpdateSql(e), entity)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildPage(ForgePageRequest r)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildCount(string baseSql, object? parameters = null) => ForgeCommand.Text($"SELECT COUNT(1)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

- `public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null)` (ForgeORM.Providers.Oracle/OracleForgeProvider.cs)

### ForgeORM.Providers.PostgreSql

- `public DbConnection CreateConnection(string connectionString) => new NpgsqlConnection(connectionString)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildInsertSql(e), entity)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildUpdateSql(e), entity)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildPage(ForgePageRequest r)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildCount(string baseSql, object? parameters = null) => ForgeCommand.Text($"SELECT COUNT(1)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

- `public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null)` (ForgeORM.Providers.PostgreSql/PostgreSqlForgeProvider.cs)

### ForgeORM.Providers.SqlServer

- `public DbConnection CreateConnection(string connectionString) => new SqlConnection(connectionString)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildInsertSql(e), entity)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildUpdateSql(e), entity)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildPage(ForgePageRequest r)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildCount(string baseSql, object? parameters = null) => ForgeCommand.Text($"SELECT COUNT(1)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

- `public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null)` (ForgeORM.Providers.SqlServer/SqlServerForgeProvider.cs)

### ForgeORM.Providers.Sqlite

- `public DbConnection CreateConnection(string connectionString) => new SqliteConnection(connectionString)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildGetById(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildGetByCode(ForgeEntityMetadata e, string code)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildGetByIds(ForgeEntityMetadata e, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildInsert(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildInsertSql(e), entity)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildUpdate(ForgeEntityMetadata e, object entity) => ForgeCommand.Text(BuildUpdateSql(e), entity)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildDelete(ForgeEntityMetadata e, object id)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildPage(ForgePageRequest r)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildCount(string baseSql, object? parameters = null) => ForgeCommand.Text($"SELECT COUNT(1)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

- `public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null)` (ForgeORM.Providers.Sqlite/SqliteForgeProvider.cs)

### ForgeORM.QueryAst

- `public sealed record ForgeRenderedSql(string Sql, object? Parameters = null)` (ForgeORM.QueryAst/Contracts.cs)

- `public sealed record ForgeCte(string Name, string Sql)` (ForgeORM.QueryAst/Contracts.cs)

- `public sealed record ForgeTempColumn(string Name, string DbType, bool Nullable)` (ForgeORM.QueryAst/Contracts.cs)

- `public IForgeAstScriptBuilder WithCte(string name, string sql)` (ForgeORM.QueryAst/ForgeAstScriptBuilder.cs)

- `public IForgeAstScriptBuilder CreateTempTable(string name, Action<IForgeAstTempTableBuilder> configure)` (ForgeORM.QueryAst/ForgeAstScriptBuilder.cs)

- `public IForgeAstScriptBuilder InsertIntoTemp(string tempTable, string selectSql)` (ForgeORM.QueryAst/ForgeAstScriptBuilder.cs)

- `public IForgeAstScriptBuilder Statement(string sql)` (ForgeORM.QueryAst/ForgeAstScriptBuilder.cs)

- `public ForgeRenderedSql Render(IForgeDatabaseProvider provider)` (ForgeORM.QueryAst/ForgeAstScriptBuilder.cs)

- `public IForgeAstTempTableBuilder Column(string name, string dbType, bool nullable = true)` (ForgeORM.QueryAst/ForgeAstScriptBuilder.cs)

- `public IForgeAstTempTableBuilder PrimaryKey(params string[] columns)` (ForgeORM.QueryAst/ForgeAstScriptBuilder.cs)

- `public ForgeTempTable Build()` (ForgeORM.QueryAst/ForgeAstScriptBuilder.cs)

- `public IForgeAstSelectBuilder<T> Columns(params Expression<Func<T, object>>[] columns)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Columns(params string[] columns)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> ColumnsSql(params string[] columns) => Columns(columns)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Distinct()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> From(string? tableName = null)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> As(string alias)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Where(Expression<Func<T, bool>> predicate)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> WhereSql(string condition, object? parameters = null)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> WhereIf(bool condition, Expression<Func<T, bool>> predicate) => condition ? Where(predicate)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> WhereSqlIf(bool condition, string sqlCondition, object? parameters = null) => condition ? WhereSql(sqlCondition, parameters)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> And(Expression<Func<T, bool>> predicate) => Where(predicate)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> AndSql(string condition, object? parameters = null) => WhereSql(condition, parameters)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Or(Expression<Func<T, bool>> predicate)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> OrSql(string condition, object? parameters = null)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Join(string table, string on) => InnerJoin(table, on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> JoinSql(string table, string on) => InnerJoinSql(table, on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> InnerJoin(string table, string on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> InnerJoinSql(string table, string on) => InnerJoin(table, on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> LeftJoin(string table, string on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> LeftJoinSql(string table, string on) => LeftJoin(table, on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> RightJoin(string table, string on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> RightJoinSql(string table, string on) => RightJoin(table, on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> FullJoin(string table, string on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> FullJoinSql(string table, string on) => FullJoin(table, on)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> CrossJoin(string table)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> CrossApply(string tableExpression, string alias)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> OuterApply(string tableExpression, string alias)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> WithCte(string name, string sql)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> WithCte(ForgeCte cte)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> GroupBy(params Expression<Func<T, object>>[] columns)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> GroupBy(params string[] columns)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> HavingSql(string condition)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Having(Expression<Func<T, bool>> predicate)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Count(string alias = "Count") => AggregateSql("COUNT(1)", alias)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Count(Expression<Func<T, object>> column, string alias = "Count")` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Sum(Expression<Func<T, object>> column, string alias = "Sum")` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Average(Expression<Func<T, object>> column, string alias = "Average")` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Min(Expression<Func<T, object>> column, string alias = "Min")` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Max(Expression<Func<T, object>> column, string alias = "Max")` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> AggregateSql(string sqlExpression, string alias)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> HavingCount(string @operator, object value) => HavingAggregateSql("COUNT(1)", @operator, value)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> HavingSum(Expression<Func<T, object>> column, string @operator, object value)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> HavingAverage(Expression<Func<T, object>> column, string @operator, object value)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> HavingMin(Expression<Func<T, object>> column, string @operator, object value)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> HavingMax(Expression<Func<T, object>> column, string @operator, object value)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> HavingAggregateSql(string aggregateSql, string @operator, object value)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Union(Action<IForgeAstSelectBuilder<T>> configure) => AddSetOperation("UNION", configure)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> UnionSql(string sql, object? parameters = null) => AddSetOperationSql("UNION", sql, parameters)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> UnionAll(Action<IForgeAstSelectBuilder<T>> configure) => AddSetOperation("UNION ALL", configure)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> UnionAllSql(string sql, object? parameters = null) => AddSetOperationSql("UNION ALL", sql, parameters)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Intersect(Action<IForgeAstSelectBuilder<T>> configure) => AddSetOperation("INTERSECT", configure)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> IntersectSql(string sql, object? parameters = null) => AddSetOperationSql("INTERSECT", sql, parameters)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Except(Action<IForgeAstSelectBuilder<T>> configure) => AddSetOperation("EXCEPT", configure)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> ExceptSql(string sql, object? parameters = null) => AddSetOperationSql("EXCEPT", sql, parameters)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> OrderBy(Expression<Func<T, object>> column)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> OrderByDescending(Expression<Func<T, object>> column)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> OrderBySql(string orderBy)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Skip(int rows)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeAstSelectBuilder<T> Take(int rows)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeRenderedSql Render(IForgeDatabaseProvider provider)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeRenderedSql RenderCount(IForgeDatabaseProvider provider)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeRenderedSql RenderAny(IForgeDatabaseProvider provider)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeRenderedSql RenderDelete(IForgeDatabaseProvider provider)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeRenderedSql RenderUpdate(IForgeDatabaseProvider provider, object values)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public System.Data.Common.DbConnection CreateConnection(string connectionString) => throw new NotSupportedException("The pass-through provider is only used to render nested AST queries.")` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildGetById(ForgeEntityMetadata entity, object id) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildGetByCode(ForgeEntityMetadata entity, string code) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildGetByIds(ForgeEntityMetadata entity, IReadOnlyCollection<int> ids) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildInsert(ForgeEntityMetadata entity, object entityInstance) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildUpdate(ForgeEntityMetadata entity, object entityInstance) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildDelete(ForgeEntityMetadata entity, object id) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildPage(ForgePageRequest request) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildCount(string baseSql, object? parameters = null) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildBulkDelete(string tableName, string keyColumn, IReadOnlyCollection<int> ids) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public ForgeCommand BuildFunctionScalar(string functionName, object? parameters = null) => throw new NotSupportedException()` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public static IReadOnlyDictionary<string, object?> Read(object parameters)` (ForgeORM.QueryAst/ForgeAstSelectBuilder.cs)

- `public IForgeDynamicSelectBuilder Select(params string[] columns) => new ForgeDynamicSelectBuilder(columns.Length == 0 ? ["*"] : columns)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder SelectAll() => new ForgeDynamicSelectBuilder(["*"])` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder Distinct()` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder From(string table)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder Where(string condition, object? parameters = null)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder And(string condition, object? parameters = null) => Where(condition, parameters)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder Or(string condition, object? parameters = null)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder Join(string table, string on) => InnerJoin(table, on)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder InnerJoin(string table, string on)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder LeftJoin(string table, string on)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder RightJoin(string table, string on)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder FullJoin(string table, string on)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder CrossJoin(string table)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder CrossApply(string tableExpression, string alias)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder OuterApply(string tableExpression, string alias)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder WithCte(string name, string sql)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder GroupBy(params string[] columns)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder Having(string condition)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder OrderBy(string orderBy)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder Skip(int rows)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public IForgeDynamicSelectBuilder Take(int rows)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public ForgeRenderedSql Build(IForgeDatabaseProvider provider) => Render(provider)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public ForgeRenderedSql Render(IForgeDatabaseProvider provider)` (ForgeORM.QueryAst/ForgeDynamicQueryBuilder.cs)

- `public ForgeRelationshipSplitQuery<TParent> From()` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public ForgeRelationshipSplitQuery<TParent> FromSql(string sql, object? parameters = null)` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public ForgeRelationshipSplitQuery<TParent> Where(Expression<Func<TParent, bool>> predicate)` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public ForgeRelationshipSplitQuery<TParent> WhereSql(string condition, object? parameters = null)` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public bool Any(string parentSql, object? parameters = null)` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public async Task<bool> AnyAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public TParent? FirstOrDefault(string parentSql, object? parameters = null) => FirstOrDefaultAsync(parentSql, parameters).GetAwaiter().GetResult()` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public async Task<TParent?> FirstOrDefaultAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default) => (await ToListAsync(parentSql, parameters, cancellationToken)).FirstOrDefault()` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public Task<IReadOnlyList<TParent>> ToListAsync(CancellationToken cancellationToken = default)` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public async Task<TParent?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) => (await ToListAsync(cancellationToken)).FirstOrDefault()` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null) => ToListAsync(parentSql, parameters).GetAwaiter().GetResult()` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public async Task<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)` (ForgeORM.QueryAst/ForgeRelationshipSplitQuery.cs)

- `public static IForgeAstScriptBuilder Script() => new ForgeAstScriptBuilder()` (ForgeORM.QueryAst/ForgeSql.cs)

- `public static IForgeAstTempTableBuilder TempTable(string name) => new ForgeAstTempTableBuilder(name)` (ForgeORM.QueryAst/ForgeSql.cs)

- `public static ForgeCte Cte(string name, string sql) => new(name, sql)` (ForgeORM.QueryAst/ForgeSql.cs)

### ForgeORM.QueryAst.Artifacts

- `public string Render()` (ForgeORM.QueryAst.Artifacts/ForgeAstArtifactExtensions.cs)

- `public ForgeViewArtifactBuilder<T> WithReason(string reason)` (ForgeORM.QueryAst.Artifacts/ForgeAstArtifactExtensions.cs)

- `public ForgeArtifactRenderResult Render(IForgeDatabaseProvider provider)` (ForgeORM.QueryAst.Artifacts/ForgeAstArtifactExtensions.cs)

- `public ForgeProcedureArtifactBuilder<T> WithParameter(string name, string dbType, string? defaultValue = null)` (ForgeORM.QueryAst.Artifacts/ForgeAstArtifactExtensions.cs)

- `public ForgeProcedureArtifactBuilder<T> WithReason(string reason)` (ForgeORM.QueryAst.Artifacts/ForgeAstArtifactExtensions.cs)

- `public ForgeArtifactRenderResult Render(IForgeDatabaseProvider provider)` (ForgeORM.QueryAst.Artifacts/ForgeAstArtifactExtensions.cs)

### ForgeORM.QueryBuilder

- `public IForgeSelectBuilder Select(params string[] columns) => new ForgeSelectBuilder(columns.Length == 0 ? ["*"] : columns)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder SelectAll() => new ForgeSelectBuilder(["*"])` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder From(string table)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder Where(string condition, object? parameters = null)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder And(string condition, object? parameters = null) => Where(condition, parameters)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder Or(string condition, object? parameters = null)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder Join(string table, string on)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder LeftJoin(string table, string on)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder OrderBy(string orderBy)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder GroupBy(params string[] columns)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder Having(string condition)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder Skip(int rows)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public IForgeSelectBuilder Take(int rows)` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public ForgeBuiltQuery Build()` (ForgeORM.QueryBuilder/ForgeDynamicQueryBuilder.cs)

- `public ForgeAdvancedQuery<T> From(string tableOrView)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> Select(params string[] columns)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> Select(params Expression<Func<T, object>>[] columns)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> LeftJoin(string table, string on)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> InnerJoin(string table, string on)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> Where(string condition, object? parameters = null)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> WhereIf(bool condition, string where, object? parameters = null) => condition ? Where(where, parameters)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> Where(Expression<Func<T, bool>> predicate)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> OrderBy(string orderBy)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> OrderByDescending(Expression<Func<T, object>> column)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> GroupBy(params string[] columns)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedQuery<T> Page(int page, int pageSize)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public ForgeAdvancedRenderedQuery Build(string provider = "SqlServer")` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

- `public sealed record ForgeAdvancedRenderedQuery(string Sql, object? Parameters)` (ForgeORM.QueryBuilder/V1V2V3/ForgeAdvancedQuery.cs)

### ForgeORM.Rag

- `public sealed record ForgeRagDocument( string Id, string Title, string Content, IReadOnlyDictionary<string, string>? Metadata = null)` (ForgeORM.Rag/Rag.cs)

- `public sealed record ForgeRagChunk( string Id, string DocumentId, string Text, int Index, IReadOnlyDictionary<string, string>? Metadata = null)` (ForgeORM.Rag/Rag.cs)

- `public sealed record ForgeRagAnswerContext( string Question, IReadOnlyList<ForgeRagChunk> Chunks, string Prompt)` (ForgeORM.Rag/Rag.cs)

- `public Task<float[]> EmbedAsync( string text, CancellationToken cancellationToken = default)` (ForgeORM.Rag/Rag.cs)

- `public async Task<IReadOnlyList<ForgeRagChunk>> IngestAsync( ForgeRagDocument document, CancellationToken cancellationToken = default)` (ForgeORM.Rag/Rag.cs)

- `public async Task<ForgeRagAnswerContext> BuildContextAsync( string question, int topK = 5, CancellationToken cancellationToken = default)` (ForgeORM.Rag/Rag.cs)

- `public static IServiceCollection AddForgeRag(this IServiceCollection services)` (ForgeORM.Rag/Rag.cs)

### ForgeORM.Realtime

- `public sealed record ForgeRealtimeEvent(string Topic, string EventName, object Payload, DateTimeOffset TimestampUtc)` (ForgeORM.Realtime/Realtime.cs)

- `public ValueTask PublishAsync(ForgeRealtimeEvent evt, CancellationToken cancellationToken = default)` (ForgeORM.Realtime/Realtime.cs)

- `public async IAsyncEnumerable<ForgeRealtimeEvent> SubscribeAsync(string topic, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)` (ForgeORM.Realtime/Realtime.cs)

- `public static IServiceCollection AddForgeRealtime(this IServiceCollection services) => services.AddSingleton<IForgeRealtimeHub, InMemoryForgeRealtimeHub>()` (ForgeORM.Realtime/Realtime.cs)

### ForgeORM.Relationships

- `public IReadOnlyList<TParent> ToList(string parentSql, object? parameters = null) => ToListAsync(parentSql, parameters).GetAwaiter().GetResult()` (ForgeORM.Relationships/ForgeRelationshipSplitQuery.cs)

- `public async Task<IReadOnlyList<TParent>> ToListAsync(string parentSql, object? parameters = null, CancellationToken cancellationToken = default)` (ForgeORM.Relationships/ForgeRelationshipSplitQuery.cs)

### ForgeORM.SchemaOps

- `public async Task EnsureHistoryTableAsync(CancellationToken cancellationToken = default)` (ForgeORM.SchemaOps/ForgeArtifactManager.cs)

- `public async Task<ForgeArtifactApplyResult> CreateOrUpdateAsync(ForgeDbArtifact artifact, CancellationToken cancellationToken = default)` (ForgeORM.SchemaOps/ForgeArtifactManager.cs)

- `public static async Task<int> ExecuteAsync(DbConnection connection, string sql, object? parameters = null, CancellationToken cancellationToken = default)` (ForgeORM.SchemaOps/ForgeArtifactManager.cs)

### ForgeORM.Security

- `public sealed record ForgeSqlSafetyResult(bool IsSafe, IReadOnlyList<string> Violations)` (ForgeORM.Security/ForgeSecurity.cs)

- `public ForgeSqlSafetyResult Validate(string sql, bool allowDdl = false, bool allowDangerous = false)` (ForgeORM.Security/ForgeSecurity.cs)

- `public string MaskEmail(string email)` (ForgeORM.Security/ForgeSecurity.cs)

- `public string MaskPhone(string phone) => Mask(phone, 2, 2)` (ForgeORM.Security/ForgeSecurity.cs)

- `public string Mask(string value, int visibleStart = 2, int visibleEnd = 2)` (ForgeORM.Security/ForgeSecurity.cs)

- `public string EncryptToBase64(string plainText, string key)` (ForgeORM.Security/ForgeSecurity.cs)

- `public string DecryptFromBase64(string cipherText, string key)` (ForgeORM.Security/ForgeSecurity.cs)

- `public static IServiceCollection AddForgeSecurity(this IServiceCollection services)` (ForgeORM.Security/ForgeSecurity.cs)

### ForgeORM.Studio.Api

- `public sealed record QueryVisualizeRequest(string Sql, string Provider = "SqlServer")` (ForgeORM.Studio.Api/Program.cs)

- `public sealed record ApiTestRequest(string Method, string Url, Dictionary<string,string>? Headers, string? Body)` (ForgeORM.Studio.Api/Program.cs)

- `public sealed record VectorSearchRequest(float[] Vector, int TopK = 5)` (ForgeORM.Studio.Api/Program.cs)

- `public sealed record AiDiagnoseRequest(string Sql, double ElapsedMs, int RowCount)` (ForgeORM.Studio.Api/Program.cs)

- `public sealed record GenerateCrudRequest(string EntityName, string RoutePrefix)` (ForgeORM.Studio.Api/Program.cs)

- `public sealed record AddColumnMigrationRequest(string Table, string Column, string SqlType, bool Nullable = true)` (ForgeORM.Studio.Api/Program.cs)

- `public sealed record ErdEntity(string Name, IReadOnlyList<string> Columns)` (ForgeORM.Studio.Api/Program.cs)

- `public sealed record ErdRelationship(string From, string To, string FromColumn, string ToColumn, string Kind)` (ForgeORM.Studio.Api/Program.cs)

- `public sealed record ErdDiagram(IReadOnlyList<ErdEntity> Entities, IReadOnlyList<ErdRelationship> Relationships)` (ForgeORM.Studio.Api/Program.cs)

- `public static IReadOnlyList<object> ExtractNodes(string sql)` (ForgeORM.Studio.Api/Program.cs)

- `public static IReadOnlyList<object> ExtractEdges(string sql)` (ForgeORM.Studio.Api/Program.cs)

### ForgeORM.Sync

- `public sealed record ForgeSyncChange(string Entity, string EntityId, string Operation, string PayloadJson, DateTimeOffset ChangedUtc, string DeviceId)` (ForgeORM.Sync/Sync.cs)

- `public sealed record ForgeSyncConflict(ForgeSyncChange Local, ForgeSyncChange Remote, string Reason)` (ForgeORM.Sync/Sync.cs)

- `public sealed record ForgeSyncResult(IReadOnlyList<ForgeSyncChange> Applied, IReadOnlyList<ForgeSyncConflict> Conflicts)` (ForgeORM.Sync/Sync.cs)

- `public Task<ForgeSyncResult> SynchronizeAsync(IReadOnlyList<ForgeSyncChange> localChanges, IReadOnlyList<ForgeSyncChange> remoteChanges, CancellationToken cancellationToken = default)` (ForgeORM.Sync/Sync.cs)

- `public Task<ForgeSyncResult> SynchronizeAsync(SyncRequest request, CancellationToken cancellationToken = default)` (ForgeORM.Sync/Sync.cs)

- `public static IServiceCollection AddForgeOfflineSync(this IServiceCollection services) => services.AddSingleton<IForgeSyncEngine, ForgeSyncEngine>()` (ForgeORM.Sync/Sync.cs)

### ForgeORM.Telemetry

- `public sealed record ForgeQueryTelemetryEvent(string Operation, string Sql, long ElapsedMilliseconds, bool Success, string? Error, DateTimeOffset TimestampUtc)` (ForgeORM.Telemetry/ForgeTelemetry.cs)

- `public sealed record ForgeMonitoringSnapshot(int TotalQueries, int FailedQueries, double AverageMilliseconds, IReadOnlyList<ForgeQueryTelemetryEvent> SlowQueries)` (ForgeORM.Telemetry/ForgeTelemetry.cs)

- `public Activity? StartQueryActivity(string operation, string sql)` (ForgeORM.Telemetry/ForgeTelemetry.cs)

- `public void RecordQuery(string operation, string sql, TimeSpan elapsed, bool success, Exception? exception = null)` (ForgeORM.Telemetry/ForgeTelemetry.cs)

- `public ForgeMonitoringSnapshot Snapshot(int slowQueryLimit = 20)` (ForgeORM.Telemetry/ForgeTelemetry.cs)

- `public static IServiceCollection AddForgeTelemetry(this IServiceCollection services)` (ForgeORM.Telemetry/ForgeTelemetry.cs)

### ForgeORM.TimeTravel

- `public sealed record TimeTravelQuery(string Entity, DateTimeOffset AsOfUtc, string? Filter = null)` (ForgeORM.TimeTravel/TimeTravel.cs)

- `public sealed record TimeTravelSql(string Sql, IReadOnlyDictionary<string,object> Parameters)` (ForgeORM.TimeTravel/TimeTravel.cs)

- `public TimeTravelSql BuildSql(TimeTravelQuery query, string provider = "SqlServer")` (ForgeORM.TimeTravel/TimeTravel.cs)

- `public static IServiceCollection AddForgeTimeTravel(this IServiceCollection services) => services.AddSingleton<IForgeTimeTravelSqlBuilder, ForgeTimeTravelSqlBuilder>()` (ForgeORM.TimeTravel/TimeTravel.cs)

### ForgeORM.VectorSearch

- `public sealed record ForgeVectorDocument( string Id, float[] Vector, string Text, IReadOnlyDictionary<string, string> Metadata)` (ForgeORM.VectorSearch/ForgeVectorSearch.cs)

- `public sealed record ForgeVectorSearchResult(string Id, string Text, double Score, IReadOnlyDictionary<string, string>? Metadata = null)` (ForgeORM.VectorSearch/ForgeVectorSearch.cs)

- `public Task UpsertAsync(ForgeVectorDocument document, CancellationToken cancellationToken = default)` (ForgeORM.VectorSearch/ForgeVectorSearch.cs)

- `public Task<IReadOnlyList<ForgeVectorSearchResult>> SearchAsync( float[] queryVector, int topK = 5, CancellationToken cancellationToken = default)` (ForgeORM.VectorSearch/ForgeVectorSearch.cs)

- `public static double CosineSimilarity(IReadOnlyList<float> a, IReadOnlyList<float> b)` (ForgeORM.VectorSearch/ForgeVectorSearch.cs)

- `public string BuildSqlServerVectorSearch(string table, string idColumn, string textColumn, string vectorColumn, int topK)` (ForgeORM.VectorSearch/ForgeVectorSearch.cs)

- `public string BuildPostgreSqlPgVectorSearch(string table, string idColumn, string textColumn, string vectorColumn, int topK)` (ForgeORM.VectorSearch/ForgeVectorSearch.cs)

- `public static IServiceCollection AddForgeInMemoryVectorSearch(this IServiceCollection services)` (ForgeORM.VectorSearch/ForgeVectorSearch.cs)

### ForgeORM.Workflow

- `public sealed record ForgeWorkflowDefinition(string Name, IReadOnlyList<ForgeWorkflowStep> Steps)` (ForgeORM.Workflow/Workflow.cs)

- `public sealed record ForgeWorkflowStep(string Name, string Kind, string Action, int RetryCount = 3, string? CompensationAction = null)` (ForgeORM.Workflow/Workflow.cs)

- `public sealed record ForgeWorkflowExecution(string Id, string WorkflowName, string Status, IReadOnlyList<ForgeWorkflowStepResult> Results)` (ForgeORM.Workflow/Workflow.cs)

- `public sealed record ForgeWorkflowStepResult(string Step, string Status, string? Error = null)` (ForgeORM.Workflow/Workflow.cs)

- `public sealed record VisualWorkflowNode(string Id, string Label, string Kind, double X, double Y)` (ForgeORM.Workflow/Workflow.cs)

- `public sealed record VisualWorkflowEdge(string From, string To, string Label = "next")` (ForgeORM.Workflow/Workflow.cs)

- `public sealed record VisualWorkflowDesignerModel(IReadOnlyList<VisualWorkflowNode> Nodes, IReadOnlyList<VisualWorkflowEdge> Edges)` (ForgeORM.Workflow/Workflow.cs)

- `public async Task<ForgeWorkflowExecution> RunAsync(ForgeWorkflowDefinition workflow, CancellationToken cancellationToken = default)` (ForgeORM.Workflow/Workflow.cs)

- `public VisualWorkflowDesignerModel ToDesignerModel(ForgeWorkflowDefinition workflow)` (ForgeORM.Workflow/Workflow.cs)

- `public static IServiceCollection AddForgeWorkflow(this IServiceCollection services) => services.AddSingleton<IForgeWorkflowEngine, ForgeWorkflowEngine>()` (ForgeORM.Workflow/Workflow.cs)
