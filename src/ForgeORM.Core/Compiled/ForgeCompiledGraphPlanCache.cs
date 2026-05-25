using System.Collections.Concurrent;
using System.Reflection;
using ForgeORM.Core;

namespace ForgeORM.Core.Compiled;

public static class ForgeCompiledGraphPlanCache
{
    private static readonly ConcurrentDictionary<Type, ForgeCompiledGraphPlan> Cache = new();

    public static ForgeCompiledGraphPlan For<TParent>(params Type[] childTypes)
        => Cache.GetOrAdd(typeof(TParent), t => new ForgeCompiledGraphPlan(t, childTypes, "CompiledGraphPlan", DateTimeOffset.UtcNow));
}
