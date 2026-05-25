using System.Collections.Concurrent;

namespace ForgeORM.Core;

public readonly record struct ForgeCompiledQueryKey(
    string Provider,
    string ResultType,
    string? ParameterType,
    string QueryFingerprint);
