using System.Threading;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Optional counters for verifying whether ForgeORM is using the framework-wide direct execution path.
/// Disabled by default so normal query execution has only one cheap boolean branch.
/// </summary>
public static class ForgeDirectExecutionDiagnostics
{
    private static long _queryHits;
    private static long _firstHits;
    private static long _singleHits;
    private static long _scalarHits;
    private static long _executeHits;
    private static long _fallbackRejects;

    /// <summary>Enable counters while benchmarking/debugging. Keep false in production hot paths.</summary>
    public static bool Enabled { get; set; }

    public static long QueryHits => Volatile.Read(ref _queryHits);
    public static long FirstHits => Volatile.Read(ref _firstHits);
    public static long SingleHits => Volatile.Read(ref _singleHits);
    public static long ScalarHits => Volatile.Read(ref _scalarHits);
    public static long ExecuteHits => Volatile.Read(ref _executeHits);
    public static long FallbackRejects => Volatile.Read(ref _fallbackRejects);

    public static void Reset()
    {
        Volatile.Write(ref _queryHits, 0);
        Volatile.Write(ref _firstHits, 0);
        Volatile.Write(ref _singleHits, 0);
        Volatile.Write(ref _scalarHits, 0);
        Volatile.Write(ref _executeHits, 0);
        Volatile.Write(ref _fallbackRejects, 0);
    }

    internal static void HitQuery()
    {
        if (Enabled) Interlocked.Increment(ref _queryHits);
    }

    internal static void HitFirst()
    {
        if (Enabled) Interlocked.Increment(ref _firstHits);
    }

    internal static void HitSingle()
    {
        if (Enabled) Interlocked.Increment(ref _singleHits);
    }

    internal static void HitScalar()
    {
        if (Enabled) Interlocked.Increment(ref _scalarHits);
    }

    internal static void HitExecute()
    {
        if (Enabled) Interlocked.Increment(ref _executeHits);
    }

    internal static void Reject()
    {
        if (Enabled) Interlocked.Increment(ref _fallbackRejects);
    }
}
