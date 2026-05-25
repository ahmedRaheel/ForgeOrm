using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public sealed record ForgeSyncChange(string Entity, string EntityId, string Operation, string PayloadJson, DateTimeOffset ChangedUtc, string DeviceId);
