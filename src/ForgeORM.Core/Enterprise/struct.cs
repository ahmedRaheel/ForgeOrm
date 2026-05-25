using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Immutable command context passed to enterprise interceptors and diagnostics.
/// </summary>
public readonly record struct ForgeCommandExecutionContext(
    string Provider,
    string Sql,
    CommandType CommandType,
    ForgeCommandOperation Operation,
    Type? ResultType,
    Type? ParameterType,
    int ParameterCount,
    string QueryFingerprint);
