using System.Linq.Expressions;
using System.Reflection;

namespace ForgeORM.Core;

/// <summary>In-memory query profiler for diagnostics and samples.</summary>
public static class ForgeQueryBuilderProfiler
{
    private static readonly List<ForgeQueryBuilderProfileEntry> Entries = [];

    public static void Record(ForgeQueryBuilderProfileEntry entry)
    {
        lock (Entries) Entries.Add(entry);
    }

    public static IReadOnlyList<ForgeQueryBuilderProfileEntry> Snapshot()
    {
        lock (Entries) return Entries.ToList();
    }

    public static void Clear()
    {
        lock (Entries) Entries.Clear();
    }
}
