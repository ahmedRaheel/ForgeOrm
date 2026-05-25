using System.Collections.Concurrent;

namespace ForgeORM.Core;

public sealed record ForgeCompiledQueryPlan(
    string Sql,
    Type ResultType,
    Type? ParameterType,
    string Provider,
    string QueryFingerprint);
