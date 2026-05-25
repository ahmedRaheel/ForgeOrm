using System.Collections.Concurrent;
using System.Reflection;
using ForgeORM.Core;

namespace ForgeORM.Core.Compiled;

/// <summary>
/// Compiled property accessor to avoid repeated reflection in hot paths.
/// </summary>
public sealed class ForgeCompiledPropertyAccessor
{
    public required string Name { get; init; }
    public required Type PropertyType { get; init; }
    public required Func<object, object?> Getter { get; init; }
    public required Action<object, object?>? Setter { get; init; }
}
