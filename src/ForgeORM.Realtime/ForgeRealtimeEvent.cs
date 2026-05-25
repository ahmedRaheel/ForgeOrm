using System.Collections.Concurrent;
using System.Threading.Channels;
using ForgeORM.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Realtime;

public sealed record ForgeRealtimeEvent(string Topic, string EventName, object Payload, DateTimeOffset TimestampUtc);
