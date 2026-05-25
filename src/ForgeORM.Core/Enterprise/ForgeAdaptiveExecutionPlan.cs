using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Adaptive execution recommendation.
/// </summary>
public sealed record ForgeAdaptiveExecutionPlan(
    string Mode,
    bool UseStreaming,
    bool UseCache,
    bool UseKeysetPaging,
    bool UseReadReplica,
    string Reason);
