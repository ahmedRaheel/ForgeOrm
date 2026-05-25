using System.Text.RegularExpressions;

namespace ForgeORM.Core.Security;

public static class ForgeTenantIsolationGuard
{
    public static ForgeTenantIsolationValidation ValidateQuery(string sql, string tenantColumn = "TenantId")
    {
        var hasTenantFilter = sql.Contains(tenantColumn, StringComparison.OrdinalIgnoreCase);
        return hasTenantFilter
            ? new ForgeTenantIsolationValidation(true, "Tenant isolation filter detected.")
            : new ForgeTenantIsolationValidation(false, $"Missing tenant isolation filter: {tenantColumn}.");
    }
}
