using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Time-series query helper foundation.
/// </summary>
public static class ForgeTimeSeriesSql
{
    public static string BucketSql(string dateColumn, ForgeTimeBucket bucket)
    {
        return bucket.Unit.ToLowerInvariant() switch
        {
            "day" => $"CAST({dateColumn} AS date)",
            "month" => $"DATEFROMPARTS(YEAR({dateColumn}), MONTH({dateColumn}), 1)",
            "hour" => $"DATEADD(hour, DATEDIFF(hour, 0, {dateColumn}), 0)",
            _ => $"CAST({dateColumn} AS date)"
        };
    }
}
