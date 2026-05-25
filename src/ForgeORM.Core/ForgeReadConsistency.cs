using System.Collections.Concurrent;

namespace ForgeORM.Core;

/// <summary>
/// Read consistency options used by the expression query builder.
/// </summary>
public enum ForgeReadConsistency
{
    Default,
    NoLock,
    Snapshot,
    UpdateLock,
    ReadPast,
    RowLock,
    ReadUncommitted
}
