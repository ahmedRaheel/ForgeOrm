using System.Collections.Concurrent;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core.Graph;

public sealed record ForgeCompiledGraphPlan(ForgeRuntimeEntityPlan Root, IReadOnlyList<ForgeCompiledGraphCollectionPlan> ChildCollections);
