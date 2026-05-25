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
