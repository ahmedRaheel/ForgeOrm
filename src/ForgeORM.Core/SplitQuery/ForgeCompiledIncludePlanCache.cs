using System.Collections.Concurrent;
using System.Reflection;

namespace ForgeORM.Core;

internal sealed record ForgeCompiledIncludePlan(
    Type RootType,
    IReadOnlyList<PropertyInfo> Includes,
    bool UseSplitQuery,
    bool UseIdentityResolution,
    string Fingerprint);

internal static class ForgeCompiledIncludePlanCache
{
    private static readonly ConcurrentDictionary<string, ForgeCompiledIncludePlan> Plans = new(StringComparer.Ordinal);

    public static ForgeCompiledIncludePlan GetOrCreate<T>(IReadOnlyList<PropertyInfo> includes, ForgeEfSplitQueryOptions options)
        => GetOrCreate(typeof(T), includes, options.Mode == ForgeEfSplitQueryMode.SplitQuery, options.UseIdentityResolution);

    public static ForgeCompiledIncludePlan GetOrCreate(Type rootType, IReadOnlyList<PropertyInfo> includes, bool splitQuery, bool identityResolution)
    {
        var fingerprint = BuildFingerprint(rootType, includes, splitQuery, identityResolution);
        return Plans.GetOrAdd(fingerprint, _ => new ForgeCompiledIncludePlan(rootType, includes.ToArray(), splitQuery, identityResolution, fingerprint));
    }

    private static string BuildFingerprint(Type rootType, IReadOnlyList<PropertyInfo> includes, bool splitQuery, bool identityResolution)
        => string.Join('|', rootType.FullName, splitQuery ? "split" : "single", identityResolution ? "identity" : "noidentity", string.Join(',', includes.Select(x => x.DeclaringType!.FullName + "." + x.Name)));
}
