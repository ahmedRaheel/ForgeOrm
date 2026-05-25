using System.Collections.Concurrent;
using System.Linq.Expressions;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

internal static class ForgeProjectionReaderCache
{
    private static readonly ConcurrentDictionary<string, ForgeProjectionPlan> Plans = new(StringComparer.Ordinal);

    public static ForgeProjectionPlan GetOrCreate(Type sourceType, Type projectionType, LambdaExpression? projection)
    {
        var fingerprint = string.Join('|', sourceType.FullName, projectionType.FullName, projection?.ToString() ?? "identity");
        return Plans.GetOrAdd(fingerprint, _ => new ForgeProjectionPlan(sourceType, projectionType, fingerprint));
    }
}
