using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ForgeORM.Core.Enterprise;

/// <summary>
/// Data virtualization registry foundation.
/// </summary>
public sealed class ForgeDataVirtualizationRegistry
{
    private readonly ConcurrentDictionary<string, ForgeVirtualDataSource> _sources = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ForgeVirtualDataSource source) => _sources[source.Name] = source;

    public IReadOnlyList<ForgeVirtualDataSource> Sources() => _sources.Values.ToList();
}
