using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public sealed record ForgeSyncResult(IReadOnlyList<ForgeSyncChange> Applied, IReadOnlyList<ForgeSyncConflict> Conflicts);
