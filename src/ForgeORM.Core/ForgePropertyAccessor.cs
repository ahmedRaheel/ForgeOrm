using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace ForgeORM.Core;

internal sealed record ForgePropertyAccessor(
    PropertyInfo Property,
    string Name,
    Type PropertyType,
    Func<object, object?>? Getter,
    Action<object, object?>? Setter);
