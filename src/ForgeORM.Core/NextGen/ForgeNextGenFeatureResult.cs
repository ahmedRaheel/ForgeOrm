using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed record ForgeNextGenFeatureResult(
    string Feature,
    string Status,
    string Description,
    IReadOnlyList<string> Capabilities,
    object? Data = null);
