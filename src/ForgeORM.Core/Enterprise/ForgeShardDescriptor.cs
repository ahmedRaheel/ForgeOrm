using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Distributed shard descriptor.
/// </summary>
public sealed record ForgeShardDescriptor(
    string Name,
    string ConnectionString,
    bool IsReadReplica = false,
    string? Region = null,
    string? TenantId = null);
