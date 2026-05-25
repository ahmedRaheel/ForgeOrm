using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed class ForgeHybridDocument
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public Dictionary<string, object?> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
}
