using System.Collections.Concurrent;
using ForgeORM.Abstractions;

namespace ForgeORM.Core;

/// <summary>
/// Default metadata resolver for Auto/SourceGenerated mode. It prefers generated metadata and falls back to reflection
/// when generation is unavailable. SourceGeneratedStrict fails fast for NativeAOT/strict deployments.
/// </summary>
public sealed class HybridForgeEntityMetadataResolver : IForgeEntityMetadataResolver
{
    private readonly ReflectionForgeEntityMetadataResolver _reflection = new();
    private readonly ConcurrentDictionary<Type, ForgeEntityMetadata> _cache = new();

    /// <summary>Resolves metadata for <typeparamref name="T"/>.</summary>
    public ForgeEntityMetadata Resolve<T>() => Resolve(typeof(T));

    /// <summary>Resolves metadata using generated metadata first and reflection fallback unless SourceGeneratedStrict mode is enabled.</summary>
    public ForgeEntityMetadata Resolve(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (_cache.TryGetValue(type, out var cached))
            return cached;

        if (ForgeSourceGeneratedRegistry.TryGetMetadata(type, out var generated))
            return _cache.GetOrAdd(type, generated);

        if (ForgeSourceGeneratedRegistry.CompilationMode == ForgeOrmCompilationMode.SourceGeneratedStrict)
            throw new InvalidOperationException(
                $"SourceGenerated mode is enabled, but no generated metadata was registered for {type.FullName}.");

        return _cache.GetOrAdd(type, _reflection.Resolve(type));
    }
}
