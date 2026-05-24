namespace ForgeORM.VectorSearch;

public interface IForgeVectorStore
/// <summary>
/// Defines the UpsertAsync operation.
/// </summary>
/// <param name="document">The document value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the UpsertAsync operation.</returns>
{
    /// <summary>
    /// Defines the UpsertAsync operation.
    /// </summary>
    /// <param name="document">The document value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the UpsertAsync operation.</returns>
    ValueTask UpsertAsync(ForgeVectorDocument document, CancellationToken cancellationToken = default);
    /// <summary>
    /// Defines the SearchAsync operation.
    /// </summary>
    /// <param name="floatqueryVector">The floatqueryVector value.</param>
    /// <param name="topK">The topK value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SearchAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeVectorSearchResult>> SearchAsync(float[] queryVector, int topK = 5, CancellationToken cancellationToken = default);
}
