using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>Central reader resolver. MSIL RuntimeEmit is the only active materialization strategy.</summary>
internal static class ForgeCompiledReaderResolver
{
    public static Func<DbDataReader, T> GetReader<T>(DbDataReader reader)
    {
        if (ForgeProviderMaterializerRegistry.TryCreateReader<T>(reader, out var providerSpecific) && providerSpecific is not null)
            return providerSpecific;

        return ForgeIlMaterializerCache.GetOrCreate<T>(reader);
    }

    public static Func<DbDataReader, object> GetReader(Type type, DbDataReader reader)
        => ForgeIlMaterializerCache.GetOrCreate(type, reader);
}
