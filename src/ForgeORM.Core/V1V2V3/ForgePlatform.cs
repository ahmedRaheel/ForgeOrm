using System.Collections.Concurrent;
using System.Text.Json;
using ForgeORM.Abstractions;
using AbstractionTenantContext = ForgeORM.Abstractions.ForgeTenantContext;
using AbstractionOutboxMessage = ForgeORM.Abstractions.ForgeOutboxMessage;

namespace ForgeORM.Core;

public static class ForgePlatform
{
    public static IReadOnlyList<ForgeModuleDescriptor> Modules { get; } =
    [
        new("V1 Core ORM", ForgeReleasePhase.V1Core,
        [
            Feature("V1-001", "Hybrid ORM engine", ForgeReleasePhase.V1Core, ForgeFeatureStatus.Ready, "Raw SQL, stored procedures, repository helpers and query builders in one API."),
            Feature("V1-002", "Expression query builder", ForgeReleasePhase.V1Core, ForgeFeatureStatus.Ready, "Expression-based filters, sorting and paging."),
            Feature("V1-003", "Provider abstraction", ForgeReleasePhase.V1Core, ForgeFeatureStatus.Ready, "Provider-oriented SQL rendering and dialect support."),
            Feature("V1-004", "Compiled query cache", ForgeReleasePhase.V1Core, ForgeFeatureStatus.Ready, "Caches generated SQL shapes for repeated execution.")
        ]),
        new("V2 Enterprise", ForgeReleasePhase.V2Enterprise,
        [
            Feature("V2-001", "Bulk operations", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Bulk insert, update, delete and merge extension points."),
            Feature("V2-002", "Multi-tenancy", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Tenant context for shared DB, schema-per-tenant or database-per-tenant strategies."),
            Feature("V2-003", "Auditing and soft delete", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Created/updated user stamps and soft delete helpers."),
            Feature("V2-004", "Outbox pattern", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Preview, "In-memory outbox store plus database integration contract."),
            Feature("V2-005", "Reporting engine", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Dynamic SQL report builder for dashboards, pivots and exports."),
            Feature("V2-006", "Caching", ForgeReleasePhase.V2Enterprise, ForgeFeatureStatus.Ready, "Cache provider contract and in-memory implementation.")
        ]),
        new("V3 AI First", ForgeReleasePhase.V3AiFirst,
        [
            Feature("V3-001", "Natural language to SQL", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.Preview, "AI query client abstraction with safe deterministic fallback."),
            Feature("V3-002", "AI query diagnostics", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.Preview, "Warnings, index hints and explanation generation."),
            Feature("V3-003", "AI CRUD API generation", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.Preview, "Generates Minimal API endpoints for a selected entity."),
            Feature("V3-004", "AI migration planning", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.ExtensionPoint, "Schema diff and migration plan extension point."),
            Feature("V3-005", "Scaffolding", ForgeReleasePhase.V3AiFirst, ForgeFeatureStatus.ExtensionPoint, "Reverse engineering extension point for database-first adoption.")
        ])
    ];

    public static IReadOnlyList<ForgeFeatureDescriptor> Features =>
        Modules.SelectMany(x => x.Features).ToList();

    private static ForgeFeatureDescriptor Feature(string code, string name, ForgeReleasePhase phase, ForgeFeatureStatus status, string description)
        => new(code, name, phase, status, description);
}

public sealed class InMemoryForgeCompiledQueryCache : IForgeCompiledQueryCache
{
    private readonly ConcurrentDictionary<ForgeCompiledQueryKey, string> _cache = new();

    /// <summary>
    /// Executes the TryGet operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the TryGet operation.</returns>
    public bool TryGet(ForgeCompiledQueryKey key, out string sql)
        => _cache.TryGetValue(key, out sql!);

    /// <summary>
    /// Executes the Set operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="sql">The sql value.</param>
    public void Set(ForgeCompiledQueryKey key, string sql)
        => _cache[key] = sql;

    /// <summary>
    /// Executes the Clear operation.
    /// </summary>
    public void Clear() => _cache.Clear();
}

public sealed class StaticForgeTenantProvider : IForgeTenantProvider
{
    /// <summary>
    /// Executes the StaticForgeTenantProvider operation.
    /// </summary>
    /// <param name="tenantId">The tenantId value.</param>
    /// <param name="connectionString">The connectionString value.</param>
    /// <param name="schema">The schema value.</param>
    /// <returns>The result of the StaticForgeTenantProvider operation.</returns>
    public StaticForgeTenantProvider(string tenantId = "default", string? connectionString = null, string? schema = null)
        => Current = new AbstractionTenantContext(tenantId, connectionString, schema);

    public AbstractionTenantContext Current { get; }
}

public sealed class SystemForgeAuditUserProvider : IForgeAuditUserProvider
{
    public string? UserId => Environment.UserName;
    public string? UserName => Environment.UserName;
}

public static class ForgeAudit
{
    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="userProvider">The userProvider value.</param>
    /// <returns>The result of the T operation.</returns>
    public static void StampCreate<T>(T entity, IForgeAuditUserProvider userProvider) where T : IForgeAuditable
    {
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.CreatedBy = userProvider.UserName ?? userProvider.UserId;
        entity.IsDeleted = false;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="userProvider">The userProvider value.</param>
    /// <returns>The result of the T operation.</returns>
    public static void StampUpdate<T>(T entity, IForgeAuditUserProvider userProvider) where T : IForgeAuditable
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.UpdatedBy = userProvider.UserName ?? userProvider.UserId;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="entity">The entity value.</param>
    /// <param name="userProvider">The userProvider value.</param>
    /// <returns>The result of the T operation.</returns>
    public static void SoftDelete<T>(T entity, IForgeAuditUserProvider userProvider) where T : IForgeAuditable
    {
        entity.IsDeleted = true;
        StampUpdate(entity, userProvider);
    }
}

public sealed class InMemoryForgeCacheProvider : IForgeCacheProvider
{
    private sealed record CacheItem(object? Value, DateTimeOffset ExpiresAt);
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_cache.TryGetValue(key, out var item))
            return Task.FromResult(default(T));

        if (item.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _cache.TryRemove(key, out _);
            return Task.FromResult(default(T));
        }

        return Task.FromResult((T?)item.Value);
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value value.</param>
    /// <param name="ttl">The ttl value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _cache[key] = new CacheItem(value, DateTimeOffset.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the RemoveAsync operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the RemoveAsync operation.</returns>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryForgeOutboxStore : IForgeOutboxStore
{
    private readonly ConcurrentDictionary<Guid, AbstractionOutboxMessage> _messages = new();

    /// <summary>
    /// Executes the EnqueueAsync operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the EnqueueAsync operation.</returns>
    public Task EnqueueAsync(AbstractionOutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the GetPendingAsync operation.
    /// </summary>
    /// <param name="take">The take value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the GetPendingAsync operation.</returns>
    public Task<IReadOnlyList<AbstractionOutboxMessage>> GetPendingAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AbstractionOutboxMessage> pending = _messages.Values
            .Where(x => x.ProcessedAt is null)
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .ToList();

        return Task.FromResult(pending);
    }

    /// <summary>
    /// Executes the MarkProcessedAsync operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the MarkProcessedAsync operation.</returns>
    public Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(id, out var message))
            _messages[id] = message with { ProcessedAt = DateTimeOffset.UtcNow };

        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="event">The event value.</param>
    /// <param name="tenantId">The tenantId value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public Task EnqueueDomainEventAsync<T>(T @event, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var message = new AbstractionOutboxMessage(
            Guid.NewGuid(),
            typeof(T).Name,
            JsonSerializer.Serialize(@event),
            DateTimeOffset.UtcNow,
            tenantId);

        return EnqueueAsync(message, cancellationToken);
    }
}

public sealed class ForgeReportingEngine : IForgeReportingEngine
{
    /// <summary>
    /// Executes the Build operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="provider">The provider value.</param>
    /// <returns>The result of the Build operation.</returns>
    public ForgeReportSql Build(ForgeReportRequest request, string provider = "SqlServer")
    {
        if (string.IsNullOrWhiteSpace(request.From))
            throw new ArgumentException("Report source table/query is required.", nameof(request));

        var columns = request.Columns.Count == 0
            ? "*"
            : string.Join(", ", request.Columns.Select(c => string.IsNullOrWhiteSpace(c.Alias)
                ? c.Expression
                : $"{c.Expression} AS {c.Alias}"));

        var sql = $"SELECT {columns} FROM {request.From}";
        if (request.Filters.Count > 0)
            sql += " WHERE " + string.Join(" AND ", request.Filters.Select(x => x.Expression));

        if (!string.IsNullOrWhiteSpace(request.GroupBy))
            sql += " GROUP BY " + request.GroupBy;

        if (!string.IsNullOrWhiteSpace(request.OrderBy))
            sql += " ORDER BY " + request.OrderBy;

        if (request.Take.HasValue)
        {
            sql += provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
                ? $" OFFSET {request.Skip ?? 0} ROWS FETCH NEXT {request.Take.Value} ROWS ONLY"
                : $" LIMIT {request.Take.Value} OFFSET {request.Skip ?? 0}";
        }

        var parameters = request.Filters
            .Where(x => x.Parameters is not null)
            .Select(x => x.Parameters)
            .ToList();

        return new ForgeReportSql(sql, parameters.Count == 0 ? null : parameters);
    }
}
