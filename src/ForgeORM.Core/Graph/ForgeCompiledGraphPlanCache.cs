using System.Collections.Concurrent;
using ForgeORM.Core.Performance;

namespace ForgeORM.Core.Graph;

/// <summary>
/// Compiled graph operation plan cache. Graph insert/update/delete now has one place to resolve entity metadata,
/// child collection metadata and write SQL instead of rebuilding reflection plans in each operation.
/// </summary>
public static class ForgeCompiledGraphPlanCache
{
    private static readonly ConcurrentDictionary<Type, ForgeCompiledGraphPlan> Cache = new();

    public static ForgeCompiledGraphPlan For<T>() => For(typeof(T));

    public static ForgeCompiledGraphPlan For(Type rootType)
        => Cache.GetOrAdd(rootType, static type =>
        {
            var root = ForgeRuntimeEntityMetadataCache.For(type);
            var childCollections = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
                .Select(p => new ForgeCompiledGraphCollectionPlan(p.Name, ResolveElementType(p.PropertyType)))
                .Where(p => p.ElementType is not null)
                .ToArray();

            return new ForgeCompiledGraphPlan(root, childCollections!);
        });

    private static Type? ResolveElementType(Type collectionType)
    {
        if (collectionType.IsArray) return collectionType.GetElementType();
        if (collectionType.IsGenericType) return collectionType.GetGenericArguments()[0];
        return collectionType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(i => i.GetGenericArguments()[0])
            .FirstOrDefault();
    }
}

public sealed record ForgeCompiledGraphPlan(ForgeRuntimeEntityPlan Root, IReadOnlyList<ForgeCompiledGraphCollectionPlan> ChildCollections);
public sealed record ForgeCompiledGraphCollectionPlan(string PropertyName, Type? ElementType);
