using System.Collections.Concurrent;

namespace ForgeORM.Core;

/// <summary>
/// Global query policies used by builders and repositories: tenant enforcement, soft delete, audit and concurrency.
/// This is deliberately centralized so enterprise filters are not scattered across SQL builder, QueryAst and graph APIs.
/// </summary>
public static class ForgeGlobalQueryPolicies
{
    private static readonly ConcurrentDictionary<Type, ForgeEntityPolicy> Policies = new();

    public static void Configure<T>(Action<ForgeEntityPolicyBuilder<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ForgeEntityPolicyBuilder<T>();
        configure(builder);
        Policies[typeof(T)] = builder.Build();
    }

    public static bool TryGet(Type type, out ForgeEntityPolicy policy) => Policies.TryGetValue(type, out policy!);

    public static ForgeEntityPolicy GetOrDefault(Type type)
        => Policies.TryGetValue(type, out var policy) ? policy : ForgeEntityPolicy.Empty;
}

public sealed class ForgeEntityPolicyBuilder<T>
{
    private string? _tenantColumn;
    private object? _tenantValue;
    private string? _softDeleteColumn;
    private object? _softDeleteActiveValue;
    private string? _createdAtColumn;
    private string? _updatedAtColumn;
    private string? _rowVersionColumn;

    public ForgeEntityPolicyBuilder<T> HasTenant(string column, object value)
    {
        _tenantColumn = column;
        _tenantValue = value;
        return this;
    }

    public ForgeEntityPolicyBuilder<T> HasSoftDelete(string column = "IsDeleted", object activeValue = null!)
    {
        _softDeleteColumn = column;
        _softDeleteActiveValue = activeValue ?? false;
        return this;
    }

    public ForgeEntityPolicyBuilder<T> HasAuditColumns(string createdAt = "CreatedAt", string updatedAt = "UpdatedAt")
    {
        _createdAtColumn = createdAt;
        _updatedAtColumn = updatedAt;
        return this;
    }

    public ForgeEntityPolicyBuilder<T> HasConcurrencyToken(string column = "RowVersion")
    {
        _rowVersionColumn = column;
        return this;
    }

    internal ForgeEntityPolicy Build() => new(_tenantColumn, _tenantValue, _softDeleteColumn, _softDeleteActiveValue, _createdAtColumn, _updatedAtColumn, _rowVersionColumn);
}

public sealed record ForgeEntityPolicy(
    string? TenantColumn,
    object? TenantValue,
    string? SoftDeleteColumn,
    object? SoftDeleteActiveValue,
    string? CreatedAtColumn,
    string? UpdatedAtColumn,
    string? RowVersionColumn)
{
    public static readonly ForgeEntityPolicy Empty = new(null, null, null, null, null, null, null);
    public bool HasTenant => !string.IsNullOrWhiteSpace(TenantColumn);
    public bool HasSoftDelete => !string.IsNullOrWhiteSpace(SoftDeleteColumn);
    public bool HasAudit => !string.IsNullOrWhiteSpace(CreatedAtColumn) || !string.IsNullOrWhiteSpace(UpdatedAtColumn);
    public bool HasConcurrency => !string.IsNullOrWhiteSpace(RowVersionColumn);
}
