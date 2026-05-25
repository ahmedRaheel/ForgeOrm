using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

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
    ValueTask AppendAsync(string streamId, IEnumerable<IForgeEvent> events, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ReadStreamAsync operation.
    /// </summary>
    /// <param name="streamId">The streamId value.</param>
    /// <param name="fromVersion">The fromVersion value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadStreamAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeStoredEvent>> ReadStreamAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the ReadAllAsync operation.
    /// </summary>
    /// <param name="fromSequence">The fromSequence value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the ReadAllAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeStoredEvent>> ReadAllAsync(long fromSequence = 0, CancellationToken cancellationToken = default);
}
