using Microsoft.Extensions.DependencyInjection;

namespace ForgeORM.Sync;

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
