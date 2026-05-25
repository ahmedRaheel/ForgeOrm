using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public sealed record ForgeSyncConflict(ForgeSyncChange Local, ForgeSyncChange Remote, string Reason);
