using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

/// <summary>
/// Describes the command operation currently being executed by ForgeORM.
/// </summary>
public enum ForgeCommandOperation
{
    Query = 0,
    FirstOrDefault = 1,
    SingleOrDefault = 2,
    Execute = 3,
    Scalar = 4,
    Stream = 5,
    Page = 6,
    Bulk = 7,
    Graph = 8
}
