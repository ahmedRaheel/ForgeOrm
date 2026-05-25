using System.Collections.Concurrent;
using System.Threading.Channels;
using ForgeORM.EventSourcing;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Realtime;

public interface IForgeRealtimeHub
/// <summary>
/// Defines the PublishAsync operation.
/// </summary>
/// <param name="evt">The evt value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the PublishAsync operation.</returns>
{
    /// <summary>
    /// Defines the PublishAsync operation.
    /// </summary>
    /// <param name="evt">The evt value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the PublishAsync operation.</returns>
    ValueTask PublishAsync(ForgeRealtimeEvent evt, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the SubscribeAsync operation.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SubscribeAsync operation.</returns>
    IAsyncEnumerable<ForgeRealtimeEvent> SubscribeAsync(string topic, CancellationToken cancellationToken = default);
}
