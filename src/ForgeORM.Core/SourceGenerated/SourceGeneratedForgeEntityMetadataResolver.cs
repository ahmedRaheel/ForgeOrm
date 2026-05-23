using System.Collections.Concurrent;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Resolves entity metadata from source-generated maps registered by the ForgeORM source generator.
/// This resolver removes reflection metadata discovery from the runtime hot path and is required for
/// NativeAOT/source-generated-only deployments.
/// </summary>
public sealed class SourceGeneratedForgeEntityMetadataResolver : IForgeEntityMetadataResolver
{
    /// <summary>Resolves source-generated metadata for <typeparamref name="T"/>.</summary>
    public ForgeEntityMetadata Resolve<T>() => Resolve(typeof(T));

    /// <summary>Resolves source-generated metadata for the supplied CLR type.</summary>
    public ForgeEntityMetadata Resolve(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (ForgeSourceGeneratedRegistry.TryGetMetadata(type, out var metadata))
            return metadata;

        throw new InvalidOperationException(
            $"No ForgeORM source-generated metadata was registered for {type.FullName}. " +
            "Add the ForgeORM source generator as an Analyzer and mark the type with [ForgeTable], [ForgeDto], [ForgeProjection], or [ForgeGenerateMapper].");
    }
}

/// <summary>
/// Default metadata resolver for Auto mode. It prefers generated metadata and falls back to reflection
/// only when generation is unavailable. In SourceGenerated mode it fails fast instead of silently using reflection.
/// </summary>
public sealed class HybridForgeEntityMetadataResolver : IForgeEntityMetadataResolver
{
    private readonly ReflectionForgeEntityMetadataResolver _reflection = new();
    private readonly ConcurrentDictionary<Type, ForgeEntityMetadata> _cache = new();

    /// <summary>Resolves metadata for <typeparamref name="T"/>.</summary>
    public ForgeEntityMetadata Resolve<T>() => Resolve(typeof(T));

    /// <summary>Resolves metadata using generated metadata first and reflection fallback only in Auto/RuntimeEmit mode.</summary>
    public ForgeEntityMetadata Resolve(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (_cache.TryGetValue(type, out var cached))
            return cached;

        if (ForgeSourceGeneratedRegistry.TryGetMetadata(type, out var generated))
            return _cache.GetOrAdd(type, generated);

        if (ForgeSourceGeneratedRegistry.CompilationMode == ForgeOrmCompilationMode.SourceGenerated)
            throw new InvalidOperationException(
                $"SourceGenerated mode is enabled, but no generated metadata was registered for {type.FullName}.");

        return _cache.GetOrAdd(type, _reflection.Resolve(type));
    }
}
