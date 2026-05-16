using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public sealed record ForgeRagDocument(
    string Id,
    string Title,
    string Content,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ForgeRagChunk(
    string Id,
    string DocumentId,
    string Text,
    int Index,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record ForgeRagAnswerContext(
    string Question,
    IReadOnlyList<ForgeRagChunk> Chunks,
    string Prompt);

public interface IForgeEmbeddingProvider
{
    Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default);
}

public sealed class DeterministicEmbeddingProvider : IForgeEmbeddingProvider
{
    private const int Dimensions = 64;

    /// <summary>
    /// Initializes or executes the EmbedAsync operation.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public Task<float[]> EmbedAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var vector = new float[Dimensions];

        foreach (var ch in text ?? string.Empty)
        {
            var idx = ((int)ch) % vector.Length;
            vector[idx] += 1f;
        }

        var length = MathF.Sqrt(vector.Sum(x => x * x));

        if (length > 0)
        {
            for (var i = 0; i < vector.Length; i++)
                vector[i] /= length;
        }

        return Task.FromResult(vector);
    }
}

public interface IForgeRagEngine
{
    Task<IReadOnlyList<ForgeRagChunk>> IngestAsync(
        ForgeRagDocument document,
        CancellationToken cancellationToken = default);

    Task<ForgeRagAnswerContext> BuildContextAsync(
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

public sealed class ForgeRagEngine : IForgeRagEngine
{
    private readonly IForgeVectorStore _vectorStore;
    private readonly IForgeEmbeddingProvider _embeddingProvider;

    /// <summary>
    /// Initializes or executes the ForgeRagEngine operation.
    /// </summary>
    /// <param name="vectorStore">The vectorStore value.</param>
    /// <param name="embeddingProvider">The embeddingProvider value.</param>
    public ForgeRagEngine(
        IForgeVectorStore vectorStore,
        IForgeEmbeddingProvider embeddingProvider)
    {
        _vectorStore = vectorStore;
        _embeddingProvider = embeddingProvider;
    }

    /// <summary>
    /// Initializes or executes the IngestAsync operation.
    /// </summary>
    /// <param name="document">The document value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<IReadOnlyList<ForgeRagChunk>> IngestAsync(
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
    /// Initializes or executes the BuildContextAsync operation.
    /// </summary>
    /// <param name="question">The question value.</param>
    /// <param name="topK">The topK value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The operation result.</returns>
    public async Task<ForgeRagAnswerContext> BuildContextAsync(
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
public sealed record RagQuestionRequest
{
    public required string Question { get; init; }

    public int TopK { get; init; } = 5;

    public string? TenantId { get; init; }

    public IReadOnlyDictionary<string, string>? Filters { get; init; }

    public bool IncludeSources { get; init; } = true;

    public bool UseSemanticRanking { get; init; } = true;

    public double? MinimumScore { get; init; }

    public CancellationToken CancellationToken { get; init; }
}
public static class ForgeRagServiceCollectionExtensions
{
    /// <summary>
    /// Initializes or executes the AddForgeRag operation.
    /// </summary>
    /// <param name="services">The services value.</param>
    /// <returns>The operation result.</returns>
    public static IServiceCollection AddForgeRag(this IServiceCollection services)
    {
        services.AddSingleton<IForgeEmbeddingProvider, DeterministicEmbeddingProvider>();
        services.AddSingleton<IForgeRagEngine, ForgeRagEngine>();

        return services;
    }
}