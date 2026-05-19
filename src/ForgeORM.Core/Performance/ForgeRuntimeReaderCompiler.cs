using System.Data.Common;

namespace ForgeORM.Core.Performance;

/// <summary>
/// Public wrapper over the internal MSIL DbDataReader materializer cache for samples and benchmarks.
/// </summary>
public static class ForgeRuntimeReaderCompiler
{
    public static Func<DbDataReader, T> GetOrCreate<T>(DbDataReader reader)
        => ForgeIlMaterializerCache.GetOrCreate<T>(reader);
}
