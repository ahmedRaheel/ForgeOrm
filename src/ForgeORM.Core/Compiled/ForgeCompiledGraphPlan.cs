using System.Collections.Concurrent;
using System.Reflection;
using ForgeORM.Core;

namespace ForgeORM.Core.Compiled;

/// <summary>
/// Compiled graph plan placeholder for source generator output.
/// </summary>
public sealed record ForgeCompiledGraphPlan(
    Type ParentType,
    IReadOnlyList<Type> ChildTypes,
    string Strategy,
    DateTimeOffset CompiledAtUtc);
