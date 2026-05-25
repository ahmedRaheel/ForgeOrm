using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Immutable command result telemetry emitted after a command completes.
/// </summary>
public readonly record struct ForgeCommandExecutionResult(
    ForgeCommandExecutionContext Context,
    TimeSpan Elapsed,
    int? RowCount,
    Exception? Exception);
