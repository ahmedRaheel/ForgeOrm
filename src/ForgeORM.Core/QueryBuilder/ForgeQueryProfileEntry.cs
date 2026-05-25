using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ForgeORM.Core;

/// <summary>
/// Captured query profile entry.
/// </summary>
public sealed class ForgeQueryProfileEntry
{
    public string Name { get; init; } = string.Empty;

    public string Entity { get; init; } = string.Empty;

    public string Sql { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, object?> Parameters { get; init; } =
        new Dictionary<string, object?>();

    public DateTimeOffset StartedAtUtc { get; init; }

    public TimeSpan Duration { get; init; }

    public int Rows { get; init; }
}
