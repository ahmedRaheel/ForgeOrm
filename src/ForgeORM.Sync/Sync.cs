using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public sealed record ForgeSyncChange(string Entity, string EntityId, string Operation, string PayloadJson, DateTimeOffset ChangedUtc, string DeviceId);
public sealed record ForgeSyncConflict(ForgeSyncChange Local, ForgeSyncChange Remote, string Reason);
public sealed record ForgeSyncResult(IReadOnlyList<ForgeSyncChange> Applied, IReadOnlyList<ForgeSyncConflict> Conflicts);

public interface IForgeSyncEngine
{
    Task<ForgeSyncResult> SynchronizeAsync(IReadOnlyList<ForgeSyncChange> localChanges, IReadOnlyList<ForgeSyncChange> remoteChanges, CancellationToken cancellationToken = default);
    Task<ForgeSyncResult> SynchronizeAsync(SyncRequest request, CancellationToken cancellationToken = default);
}

public sealed class ForgeSyncEngine : IForgeSyncEngine
{
    public Task<ForgeSyncResult> SynchronizeAsync(IReadOnlyList<ForgeSyncChange> localChanges, IReadOnlyList<ForgeSyncChange> remoteChanges, CancellationToken cancellationToken = default)
    {
        var conflicts = localChanges.Join(remoteChanges, l => (l.Entity, l.EntityId), r => (r.Entity, r.EntityId), (l, r) => new ForgeSyncConflict(l, r, "Same entity changed in two places.")).ToList();
        var conflictKeys = conflicts.Select(c => (c.Local.Entity, c.Local.EntityId)).ToHashSet();
        var applied = localChanges.Concat(remoteChanges).Where(x => !conflictKeys.Contains((x.Entity, x.EntityId))).ToList();
        return Task.FromResult(new ForgeSyncResult(applied, conflicts));
    }

    public Task<ForgeSyncResult> SynchronizeAsync(SyncRequest request, CancellationToken cancellationToken = default)
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

public static class ForgeSyncServiceCollectionExtensions
{
    public static IServiceCollection AddForgeOfflineSync(this IServiceCollection services) => services.AddSingleton<IForgeSyncEngine, ForgeSyncEngine>();
}
public sealed record SyncRequest
{
    public required string DeviceId { get; init; }

    public required string TenantId { get; init; }

    public required string UserId { get; init; }

    public DateTimeOffset LastSyncAt { get; init; }

    public IReadOnlyList<SyncEntityRequest> Entities { get; init; }
        = [];

    public bool UploadLocalChanges { get; init; } = true;

    public bool DownloadServerChanges { get; init; } = true;

    public ConflictResolutionStrategy ConflictStrategy { get; init; }
        = ConflictResolutionStrategy.ServerWins;

    public bool EnableCompression { get; init; } = true;

    public bool EnableEncryption { get; init; } = true;

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public CancellationToken CancellationToken { get; init; }
}

public sealed record SyncEntityRequest
{
    public required string EntityName { get; init; }
    public IReadOnlyList<object> LocalChanges { get; init; } = [];
    public DateTimeOffset? LastEntitySyncAt { get; init; }
}
public enum ConflictResolutionStrategy
{
    ServerWins = 1,
    ClientWins = 2,
    Merge = 3,
    Manual = 4
}
