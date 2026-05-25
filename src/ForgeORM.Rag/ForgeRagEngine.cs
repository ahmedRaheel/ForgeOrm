using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public sealed class ForgeRagEngine : IForgeRagEngine
{
    private readonly IForgeVectorStore _vectorStore;
    private readonly IForgeEmbeddingProvider _embeddingProvider;

    /// <summary>
    /// Executes the ForgeRagEngine operation.
    /// </summary>
    /// <param name="vectorStore">The vectorStore value.</param>
    /// <param name="embeddingProvider">The embeddingProvider value.</param>
    /// <returns>The result of the ForgeRagEngine operation.</returns>
    public ForgeRagEngine(
        IForgeVectorStore vectorStore,
        IForgeEmbeddingProvider embeddingProvider)
    {
        _vectorStore = vectorStore;
        _embeddingProvider = embeddingProvider;
    }

    /// <summary>
    /// Executes the IngestAsync operation.
    /// </summary>
    /// <param name="document">The document value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the IngestAsync operation.</returns>
    public async ValueTask<IReadOnlyList<ForgeRagChunk>> IngestAsync(
        ForgeRagDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var chunks = Chunk(document).ToList();

        foreach (var chunk in chunks)
        {
            if (chunk is null)
                continue;

            if (string.IsNullOrWhiteSpace(chunk.Text))
                continue;

            var vector = await _embeddingProvider.EmbedAsync(
                chunk.Text,
                cancellationToken);

            if (vector is null || vector.Length == 0)
                throw new InvalidOperationException(
                    $"Embedding provider returned null/empty vector for chunk {chunk.Id}.");

            var metadata = new Dictionary<string, string>(
                document.Metadata ?? new Dictionary<string, string>())
            {
                ["documentId"] = chunk.DocumentId,
                ["title"] = document.Title ?? string.Empty,
                ["chunkIndex"] = chunk.Index.ToString()
            };

            var vectorDocument = new ForgeVectorDocument(
                Id: chunk.Id,
                Vector: vector,
                Text: chunk.Text,
                Metadata: metadata);

            await _vectorStore.UpsertAsync(
                vectorDocument,
                cancellationToken);
        }

        return chunks;
    }

    /// <summary>
    /// Executes the BuildContextAsync operation.
    /// </summary>
    /// <param name="question">The question value.</param>
    /// <param name="topK">The topK value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the BuildContextAsync operation.</returns>
    public async ValueTask<ForgeRagAnswerContext> BuildContextAsync(
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));

        var vector = await _embeddingProvider.EmbedAsync(
            question,
            cancellationToken);

        var matches = await _vectorStore.SearchAsync(
            vector,
            topK,
            cancellationToken);

        var chunks = matches
            .Select((match, index) =>
            {
                var metadata = match.Metadata
                    ?? new Dictionary<string, string>();

                var documentId = metadata.TryGetValue("documentId", out var value)
                    ? value
                    : match.Id;

                return new ForgeRagChunk(
                    match.Id,
                    documentId,
                    match.Text,
                    index,
                    metadata);
            })
            .ToList();

        var prompt =
            "Answer using only this context:\n" +
            string.Join("\n---\n", chunks.Select(x => x.Text)) +
            $"\nQuestion: {question}";

        return new ForgeRagAnswerContext(question, chunks, prompt);
    }

    private static IEnumerable<ForgeRagChunk> Chunk(ForgeRagDocument document)
    {
        const int size = 900;

        var content = document.Content ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
            yield break;

        for (var i = 0; i < content.Length; i += size)
        {
            var length = Math.Min(size, content.Length - i);

            yield return new ForgeRagChunk(
                $"{document.Id}:{i / size}",
                document.Id,
                content.Substring(i, length),
                i / size,
                document.Metadata);
        }
    }
}
