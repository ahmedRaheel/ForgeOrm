using System.Data.Common;

namespace ForgeORM.Core;

/// <summary>
/// Central reader resolver for every public query path.
/// RuntimeEmit means RuntimeEmit only. SourceGenerated means source-generated only.
/// Auto means source-generated first, then provider-specific/runtime fallback.
/// </summary>
internal static class ForgeCompiledReaderResolver
{
    public static Func<DbDataReader, T> GetReader<T>(DbDataReader reader)
    {
        var type = typeof(T);
        var mode = ForgeSourceGeneratedRegistry.CompilationMode;

        if (mode == ForgeOrmCompilationMode.RuntimeEmit)
            return ForgeIlMaterializerCache.GetOrCreate<T>(reader);

        // SourceGenerated is an instruction to use compile-time generated code.
        // First force discovery so ModuleInitializer/registered providers from the consumer assembly
        // are visible before we decide that generation is missing.
        ForgeSourceGeneratedRegistry.DiscoverGeneratedProvidersFromLoadedAssemblies();

        if (ForgeGeneratedRegistry.TryGetReader<T>(out var registeredReader))
            return registeredReader;

        if (ForgeSourceGeneratedRegistry.TryGetOrCreateProvider(type, out var provider)
            && provider.TryCreateReader<T>(reader, out var generated)
            && generated is not null)
        {
            return generated;
        }

        if (mode == ForgeOrmCompilationMode.SourceGenerated || mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            throw new InvalidOperationException($"SourceGenerated mode failed. No source-generated reader was registered for {type.FullName}. The ForgeORM.SourceGenerators analyzer must be attached to the consuming project so a generated provider is compiled into that assembly. RuntimeEmit fallback is disabled because SourceGenerated was explicitly selected.");

        // Auto mode only: generated reader unavailable, fallback is allowed.
        if (ForgeProviderMaterializerRegistry.TryCreateReader<T>(reader, out var providerSpecific)
            && providerSpecific is not null)
        {
            return providerSpecific;
        }

        return ForgeIlMaterializerCache.GetOrCreate<T>(reader);
    }

    public static Func<DbDataReader, object> GetReader(Type type, DbDataReader reader)
    {
        var mode = ForgeSourceGeneratedRegistry.CompilationMode;

        if (mode == ForgeOrmCompilationMode.RuntimeEmit)
            return ForgeIlMaterializerCache.GetOrCreate(type, reader);

        ForgeSourceGeneratedRegistry.DiscoverGeneratedProvidersFromLoadedAssemblies();

        if (ForgeGeneratedRegistry.TryGetObjectReader(type, out var registeredReader))
            return registeredReader;

        if (ForgeSourceGeneratedRegistry.TryGetOrCreateProvider(type, out var provider))
            return provider.GetReader(type, reader);

        if (mode == ForgeOrmCompilationMode.SourceGenerated || mode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            throw new InvalidOperationException($"SourceGenerated mode failed. No source-generated reader was registered for {type.FullName}. The ForgeORM.SourceGenerators analyzer must be attached to the consuming project so a generated provider is compiled into that assembly. RuntimeEmit fallback is disabled because SourceGenerated was explicitly selected.");

        // Auto mode only: generated reader unavailable, fallback is allowed.
        return ForgeIlMaterializerCache.GetOrCreate(type, reader);
    }
}
