using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Final compiled query plan used by the high-performance execution pipeline.
/// SQL/command metadata and parameter layout are cached before execution; materializer is attached after reader shape is known.
/// </summary>
public sealed class ForgeCompiledQueryPlan<T>
{
    public required string Sql { get; init; }
    public required CommandType CommandType { get; init; }
    public required CommandBehavior Behavior { get; init; }
    public required Action<DbCommand, object?> Binder { get; init; }
    public required string[] ParameterNames { get; init; }
    public Func<DbDataReader, T>? Materializer { get; set; }
    public required string Provider { get; init; }
    public required Type? ParameterType { get; init; }
    public required string QueryFingerprint { get; init; }
    public required bool RequiresEnumNormalization { get; init; }
}
