using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Built-in interceptor that throws when a command crosses the configured slow query threshold.
/// Useful in CI/performance tests; avoid enabling it for normal production traffic unless intentional.
/// </summary>
public sealed class ForgeSlowQueryGuardInterceptor : IForgeCommandInterceptor
{
    private readonly TimeSpan _threshold;

    /// <summary>Creates a slow query guard interceptor.</summary>
    public ForgeSlowQueryGuardInterceptor(TimeSpan threshold) => _threshold = threshold;

    /// <inheritdoc />
    public ValueTask CommandExecutingAsync(DbCommand command, ForgeCommandExecutionContext context, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask CommandExecutedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken = default)
    {
        if (result.Elapsed > _threshold)
            throw new TimeoutException($"ForgeORM slow query guard failed. Elapsed={result.Elapsed.TotalMilliseconds:0.00}ms Threshold={_threshold.TotalMilliseconds:0.00}ms Fingerprint={result.Context.QueryFingerprint}");
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask CommandFailedAsync(DbCommand command, ForgeCommandExecutionResult result, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
