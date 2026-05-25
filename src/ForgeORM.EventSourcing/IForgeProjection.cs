using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.EventSourcing;

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
    ValueTask ApplyAsync(TEvent evt, CancellationToken cancellationToken = default);
}
