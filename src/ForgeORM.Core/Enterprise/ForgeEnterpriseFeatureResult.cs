using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Generic response returned by enterprise feature samples.
/// </summary>
public sealed record ForgeEnterpriseFeatureResult(
    string Feature,
    string Status,
    string Description,
    IReadOnlyList<string> Capabilities,
    object? Data = null);
