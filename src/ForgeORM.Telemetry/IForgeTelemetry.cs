using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForgeORM.Telemetry;

public interface IForgeTelemetry
/// <summary>
/// Defines the StartQueryActivity operation.
/// </summary>
/// <param name="operation">The operation value.</param>
/// <param name="sql">The sql value.</param>
/// <returns>The result of the StartQueryActivity operation.</returns>
{
    /// <summary>
    /// Defines the StartQueryActivity operation.
    /// </summary>
    /// <param name="operation">The operation value.</param>
    /// <param name="sql">The sql value.</param>
    /// <returns>The result of the StartQueryActivity operation.</returns>
    Activity? StartQueryActivity(string operation, string sql);
    /// <summary>
    /// Defines the RecordQuery operation.
    /// </summary>
    /// <param name="operation">The operation value.</param>
    /// <param name="sql">The sql value.</param>
    /// <param name="elapsed">The elapsed value.</param>
    /// <param name="success">The success value.</param>
    /// <param name="exception">The exception value.</param>
    void RecordQuery(string operation, string sql, TimeSpan elapsed, bool success, Exception? exception = null);
    /// <summary>
    /// Defines the Snapshot operation.
    /// </summary>
    /// <param name="slowQueryLimit">The slowQueryLimit value.</param>
    /// <returns>The result of the Snapshot operation.</returns>
    ForgeMonitoringSnapshot Snapshot(int slowQueryLimit = 20);
}
