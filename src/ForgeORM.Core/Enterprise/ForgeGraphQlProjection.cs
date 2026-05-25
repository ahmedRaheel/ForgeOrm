using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// GraphQL bridge model foundation.
/// </summary>
public sealed record ForgeGraphQlProjection(
    string Entity,
    IReadOnlyList<string> Fields,
    string? Filter);
