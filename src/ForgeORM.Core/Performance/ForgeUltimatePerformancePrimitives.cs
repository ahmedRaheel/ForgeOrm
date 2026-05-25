using System.Buffers;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Central place for final-stage performance primitives: struct cache keys, generated-style executor
/// registry, SQL Server TVP helpers, enum converters, pooled buffers, and NativeAOT switches.
/// These are intentionally infrastructure pieces used by normal ForgeORM APIs rather than Fast* APIs.
/// </summary>
public static class ForgeUltimatePerformancePrimitives
{
    public static bool NativeAotMode { get; set; }

    internal static QueryHashKey CreateHashKey(
        string provider,
        Type entityType,
        string sql,
        Type? projectionType = null,
        int includeHash = 0)
        => new(provider, entityType, projectionType ?? entityType, StringComparer.Ordinal.GetHashCode(sql), includeHash);

    internal static PooledArray<T> Rent<T>(int minimumLength)
        => new(ArrayPool<T>.Shared.Rent(minimumLength));
}
