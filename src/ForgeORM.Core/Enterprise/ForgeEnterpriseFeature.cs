using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Enterprise feature identifiers for advanced ForgeORM modules.
/// </summary>
public enum ForgeEnterpriseFeature
{
    DistributedQueryExecution,
    DistributedCache,
    QueryPlanAnalysis,
    AutomaticQueryOptimization,
    AdaptiveQueryExecution,
    AsyncStreaming,
    ColumnarAnalytics,
    MaterializedQueryCache,
    ChangeTrackingEventSourcing,
    DatabaseObservability,
    OpenTelemetryIntegration,
    SourceGenerators,
    BinaryProtocolOptimizations,
    AiNativeFeatures,
    AdvancedTransactions,
    GraphQLIntegration,
    DataVirtualization,
    TimeSeriesOptimization,
    EnterpriseMigrationEngine,
    EnterpriseAdminPortal
}
