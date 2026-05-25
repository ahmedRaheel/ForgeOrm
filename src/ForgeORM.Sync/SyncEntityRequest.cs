using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

public sealed record SyncEntityRequest
{
    public required string EntityName { get; init; }
    public IReadOnlyList<object> LocalChanges { get; init; } = [];
    public DateTimeOffset? LastEntitySyncAt { get; init; }
}
