using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Enterprise admin dashboard snapshot.
/// </summary>
public sealed record ForgeAdminDashboardSnapshot(
    IReadOnlyList<ForgeEnterpriseFeatureResult> Features,
    IReadOnlyList<ForgeDatabaseMetric> Metrics,
    IReadOnlyList<ForgeMaterializedQuery> MaterializedQueries);
