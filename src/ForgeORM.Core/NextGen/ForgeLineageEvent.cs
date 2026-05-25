using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed record ForgeLineageEvent(
    string Operation,
    string Entity,
    string User,
    DateTimeOffset AtUtc,
    IReadOnlyList<string> Columns);
