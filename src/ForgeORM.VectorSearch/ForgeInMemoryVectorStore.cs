namespace ForgeORM.VectorSearch;

public sealed class ForgeInMemoryVectorStore : IForgeVectorStore
{
    private readonly List<ForgeVectorDocument> _documents = [];
    private readonly object _gate = new();

    /// <summary>
    /// Executes the UpsertAsync operation.
    /// </summary>
    /// <param name="document">The document value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the UpsertAsync operation.</returns>
    public ValueTask UpsertAsync(ForgeVectorDocument document, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            _documents.RemoveAll(x => x.Id == document.Id);
            _documents.Add(document);
        }
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Executes the SearchAsync operation.
    /// </summary>
    /// <param name="floatqueryVector">The floatqueryVector value.</param>
    /// <param name="topK">The topK value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SearchAsync operation.</returns>
    public ValueTask<IReadOnlyList<ForgeVectorSearchResult>> SearchAsync(
     float[] queryVector,
     int topK = 5,
     CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryVector);

        lock (_gate)
        {
            var result = _documents
                .Select(d => new ForgeVectorSearchResult(
                    d.Id,
                    d.Text,
                    ForgeVectorMath.CosineSimilarity(queryVector, d.Vector),
                    d.Metadata))
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .ToList();

            return ValueTask.FromResult<IReadOnlyList<ForgeVectorSearchResult>>(result);
        }
    }
}
