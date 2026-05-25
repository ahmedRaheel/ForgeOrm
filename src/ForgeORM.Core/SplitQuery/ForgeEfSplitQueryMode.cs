using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal enum ForgeEfSplitQueryMode
{
    SplitQuery,
    SingleQueryRequested
}
