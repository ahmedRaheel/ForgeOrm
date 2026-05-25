using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

internal readonly record struct ForgePerformanceCommandPlanKey(string ProviderName, CommandType CommandType, string ParameterType, string SqlFingerprint);
