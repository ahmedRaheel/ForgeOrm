using System.Collections.Concurrent;

namespace ForgeORM.Core;

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
