using System.Buffers;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

internal readonly record struct QueryHashKey(
    string Provider,
    Type EntityType,
    Type ProjectionType,
    int SqlHash,
    int IncludeHash);
