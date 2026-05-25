namespace ForgeORM.Abstractions;

public interface IForgeOutboxStore
/// <summary>
/// Defines the EnqueueAsync operation.
/// </summary>
/// <param name="message">The message value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the EnqueueAsync operation.</returns>
{
    /// <summary>
    /// Defines the EnqueueAsync operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the EnqueueAsync operation.</returns>
    ValueTask EnqueueAsync(ForgeOutboxMessage message, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the GetPendingAsync operation.
    /// </summary>
    /// <param name="take">The take value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the GetPendingAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeOutboxMessage>> GetPendingAsync(int take = 100, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the MarkProcessedAsync operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the MarkProcessedAsync operation.</returns>
    ValueTask MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
}
