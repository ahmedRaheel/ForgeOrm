using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

public interface IForgeEvent { string AggregateId { get; } DateTimeOffset OccurredUtc { get; } }
public sealed record ForgeStoredEvent(long Sequence, string StreamId, string EventType, string PayloadJson, DateTimeOffset OccurredUtc);
public sealed record ForgeSnapshot(string StreamId, long Version, string PayloadJson, DateTimeOffset CreatedUtc);

public interface IForgeEventStore
/// <summary>
/// Defines the AppendAsync operation.
/// </summary>
/// <param name="streamId">The streamId value.</param>
/// <param name="events">The events value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the AppendAsync operation.</returns>
{
    /// <summary>
    /// Defines the AppendAsync operation.
    /// </summary>
    /// <param name="streamId">The streamId value.</param>
    /// <param name="events">The events value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the AppendAsync operation.</returns>
    Task AppendAsync(string streamId, IEnumerable<IForgeEvent> events, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ReadStreamAsync operation.
    /// </summary>
    /// <param name="streamId">The streamId value.</param>
    /// <param name="fromVersion">The fromVersion value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadStreamAsync operation.</returns>
    Task<IReadOnlyList<ForgeStoredEvent>> ReadStreamAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ReadAllAsync operation.
    /// </summary>
    /// <param name="fromSequence">The fromSequence value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadAllAsync operation.</returns>
    Task<IReadOnlyList<ForgeStoredEvent>> ReadAllAsync(long fromSequence = 0, CancellationToken cancellationToken = default);
}

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
    public Task AppendAsync(string streamId, IEnumerable<IForgeEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var e in events)
        {
            var seq = Interlocked.Increment(ref _sequence);
            _events.Add(new ForgeStoredEvent(seq, streamId, e.GetType().Name, System.Text.Json.JsonSerializer.Serialize(e, e.GetType()), e.OccurredUtc));
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the ReadStreamAsync operation.
    /// </summary>
    /// <param name="streamId">The streamId value.</param>
    /// <param name="fromVersion">The fromVersion value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadStreamAsync operation.</returns>
    public Task<IReadOnlyList<ForgeStoredEvent>> ReadStreamAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ForgeStoredEvent>>(_events.Where(x => x.StreamId == streamId && x.Sequence >= fromVersion).OrderBy(x => x.Sequence).ToList());

    /// <summary>
    /// Executes the ReadAllAsync operation.
    /// </summary>
    /// <param name="fromSequence">The fromSequence value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadAllAsync operation.</returns>
    public Task<IReadOnlyList<ForgeStoredEvent>> ReadAllAsync(long fromSequence = 0, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ForgeStoredEvent>>(_events.Where(x => x.Sequence >= fromSequence).OrderBy(x => x.Sequence).ToList());
}

public interface IForgeProjection<in TEvent> where TEvent : IForgeEvent
/// <summary>
/// Defines the ApplyAsync operation.
/// </summary>
/// <param name="evt">The evt value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the ApplyAsync operation.</returns>
{
    /// <summary>
    /// Defines the ApplyAsync operation.
    /// </summary>
    /// <param name="evt">The evt value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ApplyAsync operation.</returns>
    Task ApplyAsync(TEvent evt, CancellationToken cancellationToken = default);
}

public static class ForgeEventSourcingServiceCollectionExtensions
{
    /// <summary>
    /// Executes the AddForgeEventSourcing operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The result of the AddForgeEventSourcing operation.</returns>
    public static IServiceCollection AddForgeEventSourcing(this IServiceCollection services) => services.AddSingleton<IForgeEventStore, InMemoryForgeEventStore>();
}
public sealed record AppendEventRequest
{
    public required string StreamId { get; init; }

    public required string EventType { get; init; }

    public required object Data { get; init; }

    public string? TenantId { get; init; }

    public string? UserId { get; init; }

    public DateTimeOffset Timestamp { get; init; }
        = DateTimeOffset.UtcNow;

    public long? ExpectedVersion { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public CancellationToken CancellationToken { get; init; }
}
