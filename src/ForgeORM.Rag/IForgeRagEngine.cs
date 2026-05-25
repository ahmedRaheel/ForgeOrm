using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public interface IForgeRagEngine
/// <summary>
/// Defines the IngestAsync operation.
/// </summary>
/// <param name="document">The document value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the IngestAsync operation.</returns>
{
    /// <summary>
    /// Defines the IngestAsync operation.
    /// </summary>
    /// <param name="document">The document value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the IngestAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeRagChunk>> IngestAsync(
        ForgeRagDocument document,
        CancellationToken cancellationToken = default);

/// <summary>

/// Defines the BuildContextAsync operation.

/// </summary>

/// <param name="question">The question value.</param>

/// <param name="topK">The topK value.</param>

/// <param name="cancellationToken">The cancellationToken value.</param>

/// <returns>The result of the BuildContextAsync operation.</returns>

    /// <summary>
    /// Defines the BuildContextAsync operation.
    /// </summary>
    /// <param name="question">The question value.</param>
    /// <param name="topK">The topK value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the BuildContextAsync operation.</returns>
    ValueTask<ForgeRagAnswerContext> BuildContextAsync(
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
