using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Central reader resolver for every public query path. RuntimeEmit MSIL is the only hot-path materializer.
/// Source-generator discovery was intentionally removed because ForgeORM now competes with Dapper through cached RuntimeEmit plans.
/// </summary>
internal static class ForgeCompiledReaderResolver
{
    public static Func<DbDataReader, T> GetReader<T>(DbDataReader reader)
        => ForgeIlMaterializerCache.GetOrCreate<T>(reader);

    public static Func<DbDataReader, object> GetReader(Type type, DbDataReader reader)
        => ForgeIlMaterializerCache.GetOrCreate(type, reader);
}
