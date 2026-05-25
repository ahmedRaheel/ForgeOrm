using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>Registry for provider-specific typed materializers.</summary>
public static class ForgeProviderMaterializerRegistry
{
    private static readonly List<IForgeProviderMaterializer> Providers = new();
    private static readonly object Gate = new();

    public static void Register(IForgeProviderMaterializer provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (Gate) Providers.Add(provider);
    }

    public static bool TryCreateReader<T>(DbDataReader reader, out Func<DbDataReader, T>? materializer)
    {
        lock (Gate)
        {
            for (var i = 0; i < Providers.Count; i++)
            {
                if (Providers[i].TryCreateReader(reader, out materializer))
                    return true;
            }
        }

        materializer = null;
        return false;
    }
}
