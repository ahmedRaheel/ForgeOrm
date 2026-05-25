using System.Buffers;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

namespace ForgeORM.Core.Performance;

internal static class ForgeGeneratedExecutorRegistry
{
    private static readonly ConcurrentDictionary<QueryHashKey, Delegate> Executors = new();

    public static TDelegate GetOrAdd<TDelegate>(QueryHashKey key, Func<QueryHashKey, TDelegate> factory)
        where TDelegate : Delegate
        => (TDelegate)Executors.GetOrAdd(key, k => factory(k));
}
