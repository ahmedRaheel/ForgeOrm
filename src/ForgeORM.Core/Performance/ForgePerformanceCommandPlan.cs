using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core;

internal sealed record ForgePerformanceCommandPlan(string ProviderName, string Sql, CommandType CommandType, Type? ParameterType, string[] ParameterNames);
