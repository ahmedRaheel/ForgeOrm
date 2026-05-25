using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed record ForgeBackgroundJob(string Id, string Name, string Status, DateTimeOffset CreatedAtUtc);
