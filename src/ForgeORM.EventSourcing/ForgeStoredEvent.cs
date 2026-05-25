using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

public sealed record ForgeStoredEvent(long Sequence, string StreamId, string EventType, string PayloadJson, DateTimeOffset OccurredUtc);
