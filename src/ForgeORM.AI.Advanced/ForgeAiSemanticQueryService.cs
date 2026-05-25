using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public sealed class ForgeAiSemanticQueryService : IForgeAiSemanticQueryService
{
    private readonly IForgeVectorStore _store;
    /// <summary>
    /// Executes the ForgeAiSemanticQueryService operation.
    /// </summary>
    /// <param name="store">The store value.</param>
    /// <returns>The result of the ForgeAiSemanticQueryService operation.</returns>
    public ForgeAiSemanticQueryService(IForgeVectorStore store) => _store = store;

    /// <summary>
    /// Executes the SearchKnowledgeAsync operation.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SearchKnowledgeAsync operation.</returns>
    public ValueTask<IReadOnlyList<ForgeVectorSearchResult>> SearchKnowledgeAsync(string text, CancellationToken cancellationToken = default)
    {
        var vector = LocalEmbedding(text, 64);
        return _store.SearchAsync(vector, 5, cancellationToken);
    }

    private static float[] LocalEmbedding(string value, int dimensions)
    {
        var vector = new float[dimensions];
        foreach (var token in value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var hash = Math.Abs(token.ToUpperInvariant().GetHashCode());
            vector[hash % dimensions] += 1;
        }
        return vector;
    }
}
