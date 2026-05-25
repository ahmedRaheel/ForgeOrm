using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Binary protocol optimization policy foundation.
/// </summary>
public sealed record ForgeBinaryOptimizationPolicy(
    bool UseBinaryImport,
    bool ReusePreparedStatements,
    bool UseStructuredParameters);
