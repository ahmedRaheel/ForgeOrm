using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Field-level change record.
/// </summary>
public sealed record ForgeEntityChange(
    string Entity,
    string Property,
    object? OldValue,
    object? NewValue,
    DateTimeOffset ChangedAtUtc);
