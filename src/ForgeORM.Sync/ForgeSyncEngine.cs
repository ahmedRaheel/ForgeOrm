using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public sealed class ForgeSyncEngine : IForgeSyncEngine
{
    /// <summary>
    /// Executes the SynchronizeAsync operation.
    /// </summary>
    /// <param name="localChanges">The localChanges value.</param>
    /// <param name="remoteChanges">The remoteChanges value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SynchronizeAsync operation.</returns>
    public ValueTask<ForgeSyncResult> SynchronizeAsync(IReadOnlyList<ForgeSyncChange> localChanges, IReadOnlyList<ForgeSyncChange> remoteChanges, CancellationToken cancellationToken = default)
    {
        var conflicts = localChanges.Join(remoteChanges, l => (l.Entity, l.EntityId), r => (r.Entity, r.EntityId), (l, r) => new ForgeSyncConflict(l, r, "Same entity changed in two places.")).ToList();
        var conflictKeys = conflicts.Select(c => (c.Local.Entity, c.Local.EntityId)).ToHashSet();
        var applied = localChanges.Concat(remoteChanges).Where(x => !conflictKeys.Contains((x.Entity, x.EntityId))).ToList();
        return ValueTask.FromResult(new ForgeSyncResult(applied, conflicts));
    }

    /// <summary>
    /// Executes the SynchronizeAsync operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SynchronizeAsync operation.</returns>
    public ValueTask<ForgeSyncResult> SynchronizeAsync(SyncRequest request, CancellationToken cancellationToken = default)
    {
        var local = request.Entities
            .SelectMany(entity => entity.LocalChanges.Select((change, index) => new ForgeSyncChange(
                entity.EntityName,
                $"{request.DeviceId}-{entity.EntityName}-{index}",
                "Upsert",
                System.Text.Json.JsonSerializer.Serialize(change),
                DateTimeOffset.UtcNow,
                request.DeviceId)))
            .ToList();

        var remote = new List<ForgeSyncChange>();
        return SynchronizeAsync(local, remote, cancellationToken);
    }
}
