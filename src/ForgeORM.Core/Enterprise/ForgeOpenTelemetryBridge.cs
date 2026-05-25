using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

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
