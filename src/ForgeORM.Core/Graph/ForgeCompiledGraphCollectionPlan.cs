using System.Collections.Concurrent;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core.Graph;

public sealed record ForgeCompiledGraphCollectionPlan(string PropertyName, Type? ElementType);
