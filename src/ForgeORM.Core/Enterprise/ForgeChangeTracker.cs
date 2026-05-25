using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Change tracker/event sourcing helper foundation.
/// </summary>
public static class ForgeChangeTracker
{
    public static IReadOnlyList<ForgeEntityChange> Diff<T>(T before, T after)
    {
        if (before is null || after is null) return [];

        var changes = new List<ForgeEntityChange>();
        var type = typeof(T);

        foreach (var prop in type.GetProperties().Where(p => p.CanRead))
        {
            var oldValue = ForgeRuntimeAccessorCache.Get(prop, before);
            var newValue = ForgeRuntimeAccessorCache.Get(prop, after);

            if (!Equals(oldValue, newValue))
            {
                changes.Add(new ForgeEntityChange(type.Name, prop.Name, oldValue, newValue, DateTimeOffset.UtcNow));
            }
        }

        return changes;
    }
}
