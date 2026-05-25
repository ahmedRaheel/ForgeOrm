using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Query plan warning returned by the explain/analyze engine.
/// </summary>
public sealed record ForgeQueryPlanWarning(
    string Code,
    string Severity,
    string Message);
