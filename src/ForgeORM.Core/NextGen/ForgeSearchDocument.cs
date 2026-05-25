using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed record ForgeSearchDocument(string Id, string Text, IReadOnlyDictionary<string, object?> Metadata);
