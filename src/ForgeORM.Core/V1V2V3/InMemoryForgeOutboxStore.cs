using System.Collections.Concurrent;
using System.Text.Json;
using ForgeORM.Abstractions;
using AbstractionTenantContext = ForgeORM.Abstractions.ForgeTenantContext;
using AbstractionOutboxMessage = ForgeORM.Abstractions.ForgeOutboxMessage;

namespace ForgeORM.Core;

public sealed class InMemoryForgeOutboxStore : IForgeOutboxStore
{
    private readonly ConcurrentDictionary<Guid, AbstractionOutboxMessage> _messages = new();

    /// <summary>
    /// Executes the EnqueueAsync operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the EnqueueAsync operation.</returns>
    public ValueTask EnqueueAsync(AbstractionOutboxMessage message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Executes the GetPendingAsync operation.
    /// </summary>
    /// <param name="take">The take value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the GetPendingAsync operation.</returns>
    public ValueTask<IReadOnlyList<AbstractionOutboxMessage>> GetPendingAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AbstractionOutboxMessage> pending = _messages.Values
            .Where(x => x.ProcessedAt is null)
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .ToList();

        return ValueTask.FromResult(pending);
    }

    /// <summary>
    /// Executes the MarkProcessedAsync operation.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the MarkProcessedAsync operation.</returns>
    public ValueTask MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(id, out var message))
            _messages[id] = message with { ProcessedAt = DateTimeOffset.UtcNow };

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Executes the T operation.
    /// </summary>
    /// <typeparam name="T">The type used by the operation.</typeparam>
    /// <param name="event">The event value.</param>
    /// <param name="tenantId">The tenantId value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the T operation.</returns>
    public ValueTask EnqueueDomainEventAsync<T>(T @event, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var message = new AbstractionOutboxMessage(
            Guid.NewGuid(),
            typeof(T).Name,
            JsonSerializer.Serialize(@event),
            DateTimeOffset.UtcNow,
            tenantId);

        return EnqueueAsync(message, cancellationToken);
    }
}
