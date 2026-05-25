using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

internal readonly record struct ForgeParameterProperty(string Name, Func<object, object?> Getter, Type PropertyType, PropertyInfo Property);
