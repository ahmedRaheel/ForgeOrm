using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

public sealed class InMemoryForgeEventStore : IForgeEventStore
{
    private readonly ConcurrentBag<ForgeStoredEvent> _events = [];
    private long _sequence;

    /// <summary>
    /// Executes the AppendAsync operation.
    /// </summary>
    /// <param name="streamId">The streamId value.</param>
    /// <param name="events">The events value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the AppendAsync operation.</returns>
    public ValueTask AppendAsync(string streamId, IEnumerable<IForgeEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var e in events)
        {
            var seq = Interlocked.Increment(ref _sequence);
            _events.Add(new ForgeStoredEvent(seq, streamId, e.GetType().Name, System.Text.Json.JsonSerializer.Serialize(e, e.GetType()), e.OccurredUtc));
        }
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Executes the ReadStreamAsync operation.
    /// </summary>
    /// <param name="streamId">The streamId value.</param>
    /// <param name="fromVersion">The fromVersion value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadStreamAsync operation.</returns>
    public ValueTask<IReadOnlyList<ForgeStoredEvent>> ReadStreamAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IReadOnlyList<ForgeStoredEvent>>(_events.Where(x => x.StreamId == streamId && x.Sequence >= fromVersion).OrderBy(x => x.Sequence).ToList());

    /// <summary>
    /// Executes the ReadAllAsync operation.
    /// </summary>
    /// <param name="fromSequence">The fromSequence value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadAllAsync operation.</returns>
    public ValueTask<IReadOnlyList<ForgeStoredEvent>> ReadAllAsync(long fromSequence = 0, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<IReadOnlyList<ForgeStoredEvent>>(_events.Where(x => x.Sequence >= fromSequence).OrderBy(x => x.Sequence).ToList());
}
