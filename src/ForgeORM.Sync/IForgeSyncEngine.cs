using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public interface IForgeSyncEngine
/// <summary>
/// Defines the SynchronizeAsync operation.
/// </summary>
/// <param name="localChanges">The localChanges value.</param>
/// <param name="remoteChanges">The remoteChanges value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the SynchronizeAsync operation.</returns>
{
    /// <summary>
    /// Defines the SynchronizeAsync operation.
    /// </summary>
    /// <param name="localChanges">The localChanges value.</param>
    /// <param name="remoteChanges">The remoteChanges value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SynchronizeAsync operation.</returns>
    ValueTask<ForgeSyncResult> SynchronizeAsync(IReadOnlyList<ForgeSyncChange> localChanges, IReadOnlyList<ForgeSyncChange> remoteChanges, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the SynchronizeAsync operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SynchronizeAsync operation.</returns>
    ValueTask<ForgeSyncResult> SynchronizeAsync(SyncRequest request, CancellationToken cancellationToken = default);
}
