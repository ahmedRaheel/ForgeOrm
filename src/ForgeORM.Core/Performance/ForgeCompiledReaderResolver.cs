using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Central reader resolver for every public query path. SourceGenerated is preferred, RuntimeEmit MSIL is the fallback.
/// This keeps public methods unchanged while removing reflection/materializer branching from each call site.
/// </summary>
internal static class ForgeCompiledReaderResolver
{
    public static Func<DbDataReader, T> GetReader<T>(DbDataReader reader)
    {
        var type = typeof(T);
        var mode = ForgeSourceGeneratedRegistry.CompilationMode;

        if (ForgeSourceGeneratedRegistry.ShouldUseSourceGenerated &&
            ForgeSourceGeneratedRegistry.TryGetProvider(type, out var provider) &&
            provider.TryCreateReader<T>(reader, out var generated) &&
            generated is not null)
        {
            return generated;
        }

        if (mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            throw new InvalidOperationException($"No ForgeORM source-generated reader was registered for {type.FullName}.");

        if (ForgeProviderMaterializerRegistry.TryCreateReader<T>(reader, out var providerSpecific) &&
            providerSpecific is not null)
        {
            return providerSpecific;
        }

        return ForgeIlMaterializerCache.GetOrCreate<T>(reader);
    }

    public static Func<DbDataReader, object> GetReader(Type type, DbDataReader reader)
    {
        var mode = ForgeSourceGeneratedRegistry.CompilationMode;

        if (ForgeSourceGeneratedRegistry.ShouldUseSourceGenerated &&
            ForgeSourceGeneratedRegistry.TryGetProvider(type, out var provider))
        {
            return provider.GetReader(type, reader);
        }

        if (mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            throw new InvalidOperationException($"No ForgeORM source-generated reader was registered for {type.FullName}.");

        return ForgeIlMaterializerCache.GetOrCreate(type, reader);
    }
}
