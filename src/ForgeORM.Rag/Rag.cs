using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.Rag;

public sealed record ForgeRagDocument(string Id, string Title, string Content, IReadOnlyDictionary<string,string>? Metadata = null);
public sealed record ForgeRagChunk(string Id, string DocumentId, string Text, int Index, IReadOnlyDictionary<string,string>? Metadata = null);
public sealed record ForgeRagAnswerContext(string Question, IReadOnlyList<ForgeRagChunk> Chunks, string Prompt);

public interface IForgeEmbeddingProvider
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public sealed class DeterministicEmbeddingProvider : IForgeEmbeddingProvider
{
    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var vector = new float[64];
        foreach (var ch in text ?? string.Empty)
        {
            var idx = Math.Abs(ch.GetHashCode()) % vector.Length;
            vector[idx] += 1f;
        }
        var len = MathF.Sqrt(vector.Sum(x => x * x));
        if (len > 0) for (var i = 0; i < vector.Length; i++) vector[i] /= len;
        return Task.FromResult(vector);
    }
}

public interface IForgeRagEngine
{
    Task<IReadOnlyList<ForgeRagChunk>> IngestAsync(ForgeRagDocument document, CancellationToken cancellationToken = default);
    Task<ForgeRagAnswerContext> BuildContextAsync(string question, int topK = 5, CancellationToken cancellationToken = default);
}

public sealed class ForgeRagEngine(IForgeVectorStore vectorStore, IForgeEmbeddingProvider embeddingProvider) : IForgeRagEngine
{
    public async Task<IReadOnlyList<ForgeRagChunk>> IngestAsync(ForgeRagDocument document, CancellationToken cancellationToken = default)
    {
        var chunks = Chunk(document).ToList();
        foreach (var chunk in chunks)
        {
            var vector = await embeddingProvider.EmbedAsync(chunk.Text, cancellationToken);
            await vectorStore.UpsertAsync(new ForgeVectorDocument(chunk.Id, vector, chunk.Text, new Dictionary<string,string>
            {
                ["documentId"] = chunk.DocumentId,
                ["title"] = document.Title,
                ["chunkIndex"] = chunk.Index.ToString()
            }), cancellationToken);
        }
        return chunks;
    }

    public async Task<ForgeRagAnswerContext> BuildContextAsync(string question, int topK = 5, CancellationToken cancellationToken = default)
    {
        var vector = await embeddingProvider.EmbedAsync(question, cancellationToken);
        var matches = await vectorStore.SearchAsync(vector, topK, cancellationToken);
        var chunks = matches.Select((m, i) => new ForgeRagChunk(m.Document.Id, m.Document.Metadata.TryGetValue("documentId", out var d) ? d : m.Document.Id, m.Document.Text, i, m.Document.Metadata)).ToList();
        var prompt = "Answer using only this context:\n" + string.Join("\n---\n", chunks.Select(x => x.Text)) + $"\nQuestion: {question}";
        return new ForgeRagAnswerContext(question, chunks, prompt);
    }

    private static IEnumerable<ForgeRagChunk> Chunk(ForgeRagDocument document)
    {
        const int size = 900;
        var content = document.Content ?? string.Empty;
        for (var i = 0; i < content.Length; i += size)
        {
            var len = Math.Min(size, content.Length - i);
            yield return new ForgeRagChunk($"{document.Id}:{i / size}", document.Id, content.Substring(i, len), i / size, document.Metadata);
        }
    }
}

public static class ForgeRagServiceCollectionExtensions
{
    public static IServiceCollection AddForgeRag(this IServiceCollection services)
    {
        services.AddSingleton<IForgeEmbeddingProvider, DeterministicEmbeddingProvider>();
        services.AddSingleton<IForgeRagEngine, ForgeRagEngine>();
        return services;
    }
}
