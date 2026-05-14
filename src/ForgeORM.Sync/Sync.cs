using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public sealed record ForgeSyncChange(string Entity, string EntityId, string Operation, string PayloadJson, DateTimeOffset ChangedUtc, string DeviceId);
public sealed record ForgeSyncConflict(ForgeSyncChange Local, ForgeSyncChange Remote, string Reason);
public sealed record ForgeSyncResult(IReadOnlyList<ForgeSyncChange> Applied, IReadOnlyList<ForgeSyncConflict> Conflicts);

public interface IForgeSyncEngine
{
    Task<ForgeSyncResult> SynchronizeAsync(IReadOnlyList<ForgeSyncChange> localChanges, IReadOnlyList<ForgeSyncChange> remoteChanges, CancellationToken cancellationToken = default);
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
}

public static class ForgeSyncServiceCollectionExtensions
{
    public static IServiceCollection AddForgeOfflineSync(this IServiceCollection services) => services.AddSingleton<IForgeSyncEngine, ForgeSyncEngine>();
}
