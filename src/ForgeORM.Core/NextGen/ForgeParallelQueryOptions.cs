using System.Buffers;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.NextGen;

public sealed record ForgeParallelQueryOptions(int MaxDegreeOfParallelism = 4, int PartitionSize = 1000);
