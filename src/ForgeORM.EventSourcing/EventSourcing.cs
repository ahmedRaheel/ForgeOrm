using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

public interface IForgeEvent { string AggregateId { get; } DateTimeOffset OccurredUtc { get; } }
public sealed record ForgeStoredEvent(long Sequence, string StreamId, string EventType, string PayloadJson, DateTimeOffset OccurredUtc);
public sealed record ForgeSnapshot(string StreamId, long Version, string PayloadJson, DateTimeOffset CreatedUtc);

public interface IForgeEventStore
{
    Task AppendAsync(string streamId, IEnumerable<IForgeEvent> events, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForgeStoredEvent>> ReadStreamAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForgeStoredEvent>> ReadAllAsync(long fromSequence = 0, CancellationToken cancellationToken = default);
}

public sealed class InMemoryForgeEventStore : IForgeEventStore
{
    private readonly ConcurrentBag<ForgeStoredEvent> _events = [];
    private long _sequence;

    public Task AppendAsync(string streamId, IEnumerable<IForgeEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var e in events)
        {
            var seq = Interlocked.Increment(ref _sequence);
            _events.Add(new ForgeStoredEvent(seq, streamId, e.GetType().Name, System.Text.Json.JsonSerializer.Serialize(e, e.GetType()), e.OccurredUtc));
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ForgeStoredEvent>> ReadStreamAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ForgeStoredEvent>>(_events.Where(x => x.StreamId == streamId && x.Sequence >= fromVersion).OrderBy(x => x.Sequence).ToList());

    public Task<IReadOnlyList<ForgeStoredEvent>> ReadAllAsync(long fromSequence = 0, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ForgeStoredEvent>>(_events.Where(x => x.Sequence >= fromSequence).OrderBy(x => x.Sequence).ToList());
}

public interface IForgeProjection<in TEvent> where TEvent : IForgeEvent
{
    Task ApplyAsync(TEvent evt, CancellationToken cancellationToken = default);
}

public static class ForgeEventSourcingServiceCollectionExtensions
{
    public static IServiceCollection AddForgeEventSourcing(this IServiceCollection services) => services.AddSingleton<IForgeEventStore, InMemoryForgeEventStore>();
}
