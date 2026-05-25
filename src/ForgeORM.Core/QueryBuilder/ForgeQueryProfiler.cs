using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ForgeORM.Core;

/// <summary>
/// In-memory profiler used by samples and local diagnostics.
/// </summary>
public static class ForgeQueryProfiler
{
    private static readonly ConcurrentQueue<ForgeQueryProfileEntry> Entries = new();

    private const int MaxEntries = 500;

    public static void Record(ForgeQueryProfileEntry entry)
    {
        Entries.Enqueue(entry);

        while (Entries.Count > MaxEntries && Entries.TryDequeue(out _))
        {
        }
    }

    public static IReadOnlyList<ForgeQueryProfileEntry> Snapshot()
    {
        return Entries.ToArray();
    }

    public static void Clear()
    {
        while (Entries.TryDequeue(out _))
        {
        }
    }
}
