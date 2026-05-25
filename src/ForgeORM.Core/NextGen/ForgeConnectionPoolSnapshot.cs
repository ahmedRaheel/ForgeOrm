using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed record ForgeConnectionPoolSnapshot(
    int ActiveConnections,
    int IdleConnections,
    int WaitingRequests,
    DateTimeOffset CapturedAtUtc);
