namespace ForgeORM.Abstractions;

public enum ForgeReleasePhase
{
    V1Core = 1,
    V2Enterprise = 2,
    V3AiFirst = 3
}

public enum ForgeFeatureStatus
{
    Ready,
    Preview,
    ExtensionPoint,
    Planned
}

public sealed record ForgeFeatureDescriptor(
    string Code,
    string Name,
    ForgeReleasePhase Phase,
    ForgeFeatureStatus Status,
    string Description);

public sealed record ForgeModuleDescriptor(
    string Name,
    ForgeReleasePhase Phase,
    IReadOnlyList<ForgeFeatureDescriptor> Features);

public sealed record ForgeCompiledQueryKey(
    string Provider,
    string EntityName,
    string Shape,
    string? TenantId = null);

public interface IForgeCompiledQueryCache
{
    bool TryGet(ForgeCompiledQueryKey key, out string sql);
    void Set(ForgeCompiledQueryKey key, string sql);
    void Clear();
}

public sealed record ForgeTenantContext(
    string TenantId,
    string? ConnectionString = null,
    string? Schema = null);

public interface IForgeTenantProvider
{
    ForgeTenantContext Current { get; }
}

public interface IForgeAuditUserProvider
{
    string? UserId { get; }
    string? UserName { get; }
}

public interface IForgeAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
    bool IsDeleted { get; set; }
}

public sealed record ForgeOutboxMessage(
    Guid Id,
    string EventType,
    string Payload,
    DateTimeOffset CreatedAt,
    string? TenantId = null,
    DateTimeOffset? ProcessedAt = null);

public interface IForgeOutboxStore
{
    Task EnqueueAsync(ForgeOutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForgeOutboxMessage>> GetPendingAsync(int take = 100, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IForgeCacheProvider
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public sealed record ForgeReportColumn(string Name, string Expression, string? Alias = null);
public sealed record ForgeReportFilter(string Expression, object? Parameters = null);
public sealed record ForgeReportRequest(
    string Name,
    string From,
    IReadOnlyList<ForgeReportColumn> Columns,
    IReadOnlyList<ForgeReportFilter> Filters,
    string? GroupBy = null,
    string? OrderBy = null,
    int? Skip = null,
    int? Take = null);

public sealed record ForgeReportSql(string Sql, object? Parameters);

public interface IForgeReportingEngine
{
    ForgeReportSql Build(ForgeReportRequest request, string provider = "SqlServer");
}

public sealed record ForgeAiQueryRequest(
    string Prompt,
    string Provider,
    string? EntityName = null,
    string? Schema = null,
    string? SafetyPolicy = null);

public sealed record ForgeAiQueryResult(
    string Sql,
    string Explanation,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> SuggestedIndexes);

public interface IForgeAiQueryClient
{
    Task<ForgeAiQueryResult> GenerateSqlAsync(ForgeAiQueryRequest request, CancellationToken cancellationToken = default);
}

public sealed record ForgeScaffoldRequest(string ConnectionString, string Provider, string Namespace, string OutputPath);
public sealed record ForgeGeneratedFile(string Path, string Content);

public interface IForgeSchemaScaffolder
{
    Task<IReadOnlyList<ForgeGeneratedFile>> ScaffoldAsync(ForgeScaffoldRequest request, CancellationToken cancellationToken = default);
}

public sealed record ForgeApiGenerationRequest(string EntityName, string RoutePrefix, string Namespace);
public interface IForgeApiGenerator
{
    IReadOnlyList<ForgeGeneratedFile> GenerateCrudApi(ForgeApiGenerationRequest request);
}

public sealed record ForgeMigrationPlan(string Name, IReadOnlyList<string> UpSql, IReadOnlyList<string> DownSql);
public interface IForgeMigrationPlanner
{
    ForgeMigrationPlan Plan(string name, IReadOnlyList<string> currentSchema, IReadOnlyList<string> targetSchema);
}
