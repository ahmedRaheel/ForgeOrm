using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Source-generator registration model foundation.
/// </summary>
public sealed record ForgeGeneratedArtifact(
    string Name,
    string Kind,
    string Path,
    string Description);
