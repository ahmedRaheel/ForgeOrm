using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

public sealed record ForgeSnapshot(string StreamId, long Version, string PayloadJson, DateTimeOffset CreatedUtc);
