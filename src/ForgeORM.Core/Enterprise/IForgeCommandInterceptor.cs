using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Enterprise interception hook for auditing, tracing, multi-tenant enforcement,
/// slow query analysis, OpenTelemetry bridges and custom command policies.
/// Implementations should avoid heavy work in hot paths.
/// </summary>
public interface IForgeCommandInterceptor
{
    /// <summary>Runs before the command is executed.</summary>
    ValueTask CommandExecutingAsync(DbCommand command, ForgeCommandExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>Runs after the command completes successfully.</summary>
    ValueTask CommandExecutedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken = default);

    /// <summary>Runs when the command fails.</summary>
    ValueTask CommandFailedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken = default);
}
