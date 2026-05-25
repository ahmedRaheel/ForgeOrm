using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Core;

internal sealed record PropertyAccessorPlan(
    Type Type,
    IReadOnlyList<ForgePropertyAccessor> Properties,
    IReadOnlyDictionary<string, ForgePropertyAccessor> ByName);
