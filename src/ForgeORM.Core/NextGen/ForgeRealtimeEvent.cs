using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed record ForgeRealtimeEvent(string Channel, string Payload, DateTimeOffset CreatedAtUtc);
