using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public interface IForgeEmbeddingProvider
/// <summary>
/// Defines the EmbedAsync operation.
/// </summary>
/// <param name="text">The text value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the EmbedAsync operation.</returns>
{
    /// <summary>
    /// Defines the EmbedAsync operation.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the EmbedAsync operation.</returns>
    ValueTask<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default);
}
