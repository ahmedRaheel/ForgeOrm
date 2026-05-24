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
/// <summary>
/// Defines the TryGet operation.
/// </summary>
/// <param name="key">The key value.</param>
/// <param name="sql">The sql value.</param>
/// <returns>The result of the TryGet operation.</returns>
{
    /// <summary>
    /// Defines the TryGet operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the TryGet operation.</returns>
    bool TryGet(ForgeCompiledQueryKey key, out string sql);
    /// <summary>
    /// Defines the Set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="sql">The sql value.</param>
    void Set(ForgeCompiledQueryKey key, string sql);
    /// <summary>
    /// Defines the Clear operation.
    /// </summary>
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
/// <summary>
/// Defines the EnqueueAsync operation.
/// </summary>
/// <param name="message">The message value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the EnqueueAsync operation.</returns>
{
    /// <summary>
    /// Defines the EnqueueAsync operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the EnqueueAsync operation.</returns>
    ValueTask EnqueueAsync(ForgeOutboxMessage message, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the GetPendingAsync operation.
    /// </summary>
    /// <param name="take">The take value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the GetPendingAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeOutboxMessage>> GetPendingAsync(int take = 100, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the MarkProcessedAsync operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the MarkProcessedAsync operation.</returns>
    ValueTask MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IForgeCacheProvider
/// <summary>
/// Defines the T operation.
/// </summary>
/// <typeparam name="T">The type used by the operation.</typeparam>
/// <param name="key">The key value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the T operation.</returns>
{
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value value.</param>
    /// <param name="ttl">The ttl value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    ValueTask SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the RemoveAsync operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RemoveAsync operation.</returns>
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
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
/// <summary>
/// Defines the Build operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <param name="provider">The provider value.</param>
/// <returns>The result of the Build operation.</returns>
{
    /// <summary>
    /// Defines the Build operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Build operation.</returns>
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
/// <summary>
/// Defines the GenerateSqlAsync operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the GenerateSqlAsync operation.</returns>
{
    /// <summary>
    /// Defines the GenerateSqlAsync operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the GenerateSqlAsync operation.</returns>
    ValueTask<ForgeAiQueryResult> GenerateSqlAsync(ForgeAiQueryRequest request, CancellationToken cancellationToken = default);
}

public sealed record ForgeScaffoldRequest(string ConnectionString, string Provider, string Namespace, string OutputPath);
public sealed record ForgeGeneratedFile(string Path, string Content);

public interface IForgeSchemaScaffolder
/// <summary>
/// Defines the ScaffoldAsync operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the ScaffoldAsync operation.</returns>
{
    /// <summary>
    /// Defines the ScaffoldAsync operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ScaffoldAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeGeneratedFile>> ScaffoldAsync(ForgeScaffoldRequest request, CancellationToken cancellationToken = default);
}

public sealed record ForgeApiGenerationRequest(string EntityName, string RoutePrefix, string Namespace);
public interface IForgeApiGenerator
/// <summary>
/// Defines the GenerateCrudApi operation.
/// </summary>
/// <param name="request">The request value.</param>
/// <returns>The result of the GenerateCrudApi operation.</returns>
{
    /// <summary>
    /// Defines the GenerateCrudApi operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <returns>The result of the GenerateCrudApi operation.</returns>
    IReadOnlyList<ForgeGeneratedFile> GenerateCrudApi(ForgeApiGenerationRequest request);
}

public sealed record ForgeMigrationPlan(string Name, IReadOnlyList<string> UpSql, IReadOnlyList<string> DownSql);
public interface IForgeMigrationPlanner
/// <summary>
/// Defines the Plan operation.
/// </summary>
/// <param name="name">The name value.</param>
/// <param name="currentSchema">The currentSchema value.</param>
/// <param name="targetSchema">The targetSchema value.</param>
/// <returns>The result of the Plan operation.</returns>
{
    /// <summary>
    /// Defines the Plan operation.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="currentSchema">The currentSchema value.</param>
    /// <param name="targetSchema">The targetSchema value.</param>
    /// <returns>The result of the Plan operation.</returns>
    ForgeMigrationPlan Plan(string name, IReadOnlyList<string> currentSchema, IReadOnlyList<string> targetSchema);
}
