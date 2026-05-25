using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ForgeORM.VectorSearch;

namespace ForgeORM.AI.Advanced;

public interface IForgeAiSemanticQueryService
/// <summary>
/// Defines the SearchKnowledgeAsync operation.
/// </summary>
/// <param name="text">The text value.</param>
/// <param name="cancellationToken">The cancellationToken value.</param>
/// <returns>The result of the SearchKnowledgeAsync operation.</returns>
{
    /// <summary>
    /// Defines the SearchKnowledgeAsync operation.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <param name="cancellationToken">The cancellationToken value.</param>
    /// <returns>The result of the SearchKnowledgeAsync operation.</returns>
    ValueTask<IReadOnlyList<ForgeVectorSearchResult>> SearchKnowledgeAsync(string text, CancellationToken cancellationToken = default);
}
