using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using ForgeORM.Abstractions;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Cached property accessor with MSIL getter/setter delegates.
/// </summary>
public sealed class ForgeRuntimePropertyPlan
{
    public required string PropertyName { get; init; }
    public required string ColumnName { get; init; }
    public required Type PropertyType { get; init; }
    public required bool IsKey { get; init; }
    public required bool IsComputed { get; init; }
    public required Func<object, object?> Getter { get; init; }
    public required Action<object, object?>? Setter { get; init; }
}
