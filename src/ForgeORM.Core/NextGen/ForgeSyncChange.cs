using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed record ForgeSyncChange(string Entity, string Key, string Operation, DateTimeOffset ChangedAtUtc);
