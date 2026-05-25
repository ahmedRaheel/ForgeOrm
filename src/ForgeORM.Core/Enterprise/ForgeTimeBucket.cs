using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Time series bucket definition.
/// </summary>
public sealed record ForgeTimeBucket(
    string Unit,
    int Size);
