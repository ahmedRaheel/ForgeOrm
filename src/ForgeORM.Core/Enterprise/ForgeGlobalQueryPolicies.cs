using System.Collections.Concurrent;

namespace ForgeORM.Core;

/// <summary>
/// Global query policies used by builders and repositories: tenant enforcement, soft delete, audit and concurrency.
/// This is deliberately centralized so enterprise filters are not scattered across SQL builder, QueryAst and graph APIs.
/// </summary>
public static class ForgeGlobalQueryPolicies
{
    private static readonly ConcurrentDictionary<Type, ForgeEntityPolicy> Policies = new();

    public static void Configure<T>(Action<ForgeEntityPolicyBuilder<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var builder = new ForgeEntityPolicyBuilder<T>();
        configure(builder);
        Policies[typeof(T)] = builder.Build();
    }

    public static bool TryGet(Type type, out ForgeEntityPolicy policy) => Policies.TryGetValue(type, out policy!);

    public static ForgeEntityPolicy GetOrDefault(Type type)
        => Policies.TryGetValue(type, out var policy) ? policy : ForgeEntityPolicy.Empty;
}
