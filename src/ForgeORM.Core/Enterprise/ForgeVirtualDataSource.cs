using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Virtual data source descriptor.
/// </summary>
public sealed record ForgeVirtualDataSource(
    string Name,
    string Kind,
    string ConnectionOrEndpoint);
