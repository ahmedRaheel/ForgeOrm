using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

public interface IForgeEvent { string AggregateId { get; } DateTimeOffset OccurredUtc { get; } }
