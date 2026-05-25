using System.Collections.Concurrent;

namespace ForgeORM.Core;

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
